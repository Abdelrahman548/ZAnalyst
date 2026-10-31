using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ZAnalyst.Desktop.ViewModels;
using ZAnalyst.Infrastructure;
using System.IO;

namespace ZAnalyst.Desktop;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _serviceProvider;
    private AsyncServiceScope? _desktopScope;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZAnalyst");
            Directory.CreateDirectory(dataDirectory);

            var services = new ServiceCollection();
            services.AddZAnalystInfrastructure(
                $"Data Source={Path.Combine(dataDirectory, "zanalyst.db")}");
            services.AddScoped<ShellViewModel>();
            services.AddScoped<MainWindow>();
            services.AddTransient<MatchAnalysisWindow>();

            _serviceProvider = services.BuildServiceProvider();
            desktop.Exit += async (_, _) =>
            {
                if (_desktopScope is { } scope)
                    await scope.DisposeAsync();
                _serviceProvider?.Dispose();
            };
            _ = StartDesktopAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var provider = _serviceProvider
                ?? throw new InvalidOperationException("Desktop services have not been initialized.");
            _desktopScope = provider.CreateAsyncScope();
            var shell = _desktopScope.Value.ServiceProvider.GetRequiredService<ShellViewModel>();
            desktop.MainWindow = _desktopScope.Value.ServiceProvider.GetRequiredService<MainWindow>();

            await provider.InitializeZAnalystDatabaseAsync();
            await shell.InitializeAsync();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not initialize the local workspace: {exception}");
            desktop.Shutdown(1);
        }
    }
}