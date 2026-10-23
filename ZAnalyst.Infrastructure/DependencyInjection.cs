using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Application.Services;
using ZAnalyst.Infrastructure.Persistence;
using ZAnalyst.Infrastructure.Repositories;

namespace ZAnalyst.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddZAnalystInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ZAnalystDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ZAnalystDbContext>());
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IActionRepository, ActionRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<IButtonRepository, ButtonRepository>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IActionService, ActionService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IButtonService, ButtonService>();

        return services;
    }

    public static async Task InitializeZAnalystDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ZAnalystDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
