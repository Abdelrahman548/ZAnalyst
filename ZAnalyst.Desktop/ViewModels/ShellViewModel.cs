using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private WorkspacePageViewModel _currentPage;

    public ShellViewModel()
    {
        NavigateCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationItemViewModel item)
                NavigateTo(item);
        });

        NavigationItems =
        [
            CreateNavigationItem("Matches", "01", new MatchesViewModel()),
            CreateNavigationItem("Match analysis", "02", new MatchAnalysisViewModel()),
            CreateNavigationItem("Teams & players", "03", new TeamsViewModel()),
            CreateNavigationItem("Templates", "04", new TemplatesViewModel())
        ];

        _currentPage = NavigationItems[0].Page;
        NavigationItems[0].IsSelected = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public ICommand NavigateCommand { get; }

    public WorkspacePageViewModel CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (ReferenceEquals(_currentPage, value))
                return;

            _currentPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTitle));
            OnPropertyChanged(nameof(CurrentDescription));
        }
    }

    public string CurrentTitle => CurrentPage.Title;
    public string CurrentDescription => CurrentPage.Description;

    private NavigationItemViewModel CreateNavigationItem(
        string label,
        string shortcut,
        WorkspacePageViewModel page) =>
        new(label, shortcut, page, NavigateCommand);

    private void NavigateTo(NavigationItemViewModel selectedItem)
    {
        foreach (var item in NavigationItems)
            item.IsSelected = ReferenceEquals(item, selectedItem);

        CurrentPage = selectedItem.Page;
        OnPropertyChanged(nameof(NavigationItems));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class RelayCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);
    }
}
