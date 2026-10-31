using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ZAnalyst.Desktop.ViewModels;

namespace ZAnalyst.Desktop;

public partial class MatchAnalysisWindow : Window
{
    private MatchAnalysisViewModel? _viewModel;
    private readonly HashSet<Key> _pressedKeys = [];

    public MatchAnalysisWindow()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
        AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, HandleKeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => _pressedKeys.Clear();
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

    private void HandleKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Source is TextBox or ComboBox || !_pressedKeys.Add(eventArgs.Key))
            return;

        var modifiers = eventArgs.KeyModifiers;
        if (modifiers == KeyModifiers.None && eventArgs.Key is Key.Left or Key.Right)
        {
            _viewModel?.SkipPlayback(eventArgs.Key == Key.Left ? -5 : 5);
            eventArgs.Handled = true;
            return;
        }

        var shortcutParts = new List<string>(4);
        if (modifiers.HasFlag(KeyModifiers.Control))
            shortcutParts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Alt))
            shortcutParts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Shift))
            shortcutParts.Add("Shift");
        shortcutParts.Add(eventArgs.Key.ToString());

        if (_viewModel?.TryRecordHotkey(string.Join("+", shortcutParts)) == true)
            eventArgs.Handled = true;
    }

    private void HandleKeyUp(object? sender, KeyEventArgs eventArgs) =>
        _pressedKeys.Remove(eventArgs.Key);
}
