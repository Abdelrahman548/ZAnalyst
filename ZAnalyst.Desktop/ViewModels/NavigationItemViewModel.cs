using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class NavigationItemViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public NavigationItemViewModel(string label, string shortcut, WorkspacePageViewModel page, ICommand command)
    {
        Label = label;
        Shortcut = shortcut;
        Page = page;
        Command = command;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Label { get; }
    public string Shortcut { get; }
    public WorkspacePageViewModel Page { get; }
    public ICommand Command { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}
