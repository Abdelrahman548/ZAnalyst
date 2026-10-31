using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ZAnalyst.Desktop.ViewModels;

namespace ZAnalyst.Desktop.Views;

public partial class MatchesView : UserControl
{
    public MatchesView()
    {
        InitializeComponent();
    }

    private async void BrowseVideo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not MatchesViewModel viewModel)
            return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this)
                ?? throw new InvalidOperationException("The match view is not attached to a window.");
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select match video",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Video files")
                    {
                        Patterns = ["*.mp4", "*.mkv", "*.mov", "*.avi", "*.webm"]
                    }
                ]
            });

            if (files.Count > 0)
                viewModel.SetVideoPath(files[0].Path.LocalPath);
        }
        catch (Exception exception)
        {
            viewModel.ReportVideoPickerError(exception);
        }
    }
}
