using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private WorkspacePageViewModel _currentPage;

    private readonly TeamsViewModel _teamsViewModel;
    private readonly TemplatesViewModel _templatesViewModel;
    private readonly MatchesViewModel _matchesViewModel;
    private readonly MatchAnalysisViewModel _matchAnalysisViewModel;

    public ShellViewModel(
        ITeamService teamService,
        IPlayerService playerService,
        IMatchService matchService,
        ITemplateService templateService,
        IButtonService buttonService,
        IEventService eventService,
        IActionService actionService)
    {
        _teamsViewModel = new TeamsViewModel(teamService, playerService);
        _templatesViewModel = new TemplatesViewModel(templateService, buttonService);
        _matchesViewModel = new MatchesViewModel(matchService, teamService, templateService);
        _matchAnalysisViewModel = new MatchAnalysisViewModel(
            eventService, actionService, buttonService, teamService);
        _matchesViewModel.MatchSelected += OpenMatchAnalysis;
        NavigateCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationItemViewModel item)
                NavigateTo(item);
        });

        NavigationItems =
        [
            CreateNavigationItem("Matches", "01", _matchesViewModel),
            CreateNavigationItem("Teams & players", "02", _teamsViewModel),
            CreateNavigationItem("Templates", "03", _templatesViewModel)
        ];

        _currentPage = NavigationItems[0].Page;
        NavigationItems[0].IsSelected = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<MatchAnalysisViewModel>? MatchAnalysisWindowRequested;

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

    public async Task InitializeAsync()
    {
        await _teamsViewModel.InitializeAsync();
        await _templatesViewModel.InitializeAsync();
        await _matchesViewModel.InitializeAsync();
    }

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

    private async void OpenMatchAnalysis(Match match)
    {
        await _matchAnalysisViewModel.LoadMatchAsync(match);
        MatchAnalysisWindowRequested?.Invoke(_matchAnalysisViewModel);
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
