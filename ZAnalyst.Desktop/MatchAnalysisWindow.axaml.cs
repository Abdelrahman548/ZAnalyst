using Avalonia.Controls;
using ZAnalyst.Desktop.ViewModels;

namespace ZAnalyst.Desktop;

public partial class MatchAnalysisWindow : Window
{
    private MatchAnalysisViewModel? _viewModel;

    public MatchAnalysisWindow()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
    }

    public MatchAnalysisWindow(MatchAnalysisViewModel viewModel)
        : this()
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    private void HandleDataContextChanged(object? sender, EventArgs eventArgs)
    {
        if (_viewModel is not null)
            _viewModel.BackRequested -= Close;

        _viewModel = DataContext as MatchAnalysisViewModel;
        if (_viewModel is not null)
            _viewModel.BackRequested += Close;
    }
}
