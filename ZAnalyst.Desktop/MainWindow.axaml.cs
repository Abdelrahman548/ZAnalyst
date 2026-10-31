using Avalonia.Controls;

namespace ZAnalyst.Desktop;

public partial class MainWindow : Window
{
    private MatchAnalysisWindow? _matchAnalysisWindow;
    private ViewModels.ShellViewModel? _shellViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
    }

    public MainWindow(ViewModels.ShellViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private void HandleDataContextChanged(object? sender, EventArgs eventArgs)
    {
        if (_shellViewModel is not null)
            _shellViewModel.MatchAnalysisWindowRequested -= OpenMatchAnalysisWindow;

        _shellViewModel = DataContext as ViewModels.ShellViewModel;
        if (_shellViewModel is not null)
            _shellViewModel.MatchAnalysisWindowRequested += OpenMatchAnalysisWindow;
    }

    private void OpenMatchAnalysisWindow(ViewModels.MatchAnalysisViewModel viewModel)
    {
        if (_matchAnalysisWindow is not null)
        {
            _matchAnalysisWindow.Activate();
            return;
        }

        _matchAnalysisWindow = new MatchAnalysisWindow(viewModel);
        _matchAnalysisWindow.Closed += (_, _) =>
        {
            _matchAnalysisWindow = null;
            Show();
            Activate();
        };
        Hide();
        _matchAnalysisWindow.Show();
        _matchAnalysisWindow.Activate();
    }
}