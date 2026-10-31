using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class MatchesViewModel : WorkspacePageViewModel, INotifyPropertyChanged
{
    private readonly IMatchService _matchService;
    private readonly ITeamService _teamService;
    private readonly ITemplateService _templateService;
    private Team? _selectedTeam;
    private Template? _selectedTemplate;
    private string? _videoPath;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isCreateDialogOpen;
    private bool _isDeleteDialogOpen;
    private Match? _pendingDeleteMatch;

    public MatchesViewModel(
        IMatchService matchService,
        ITeamService teamService,
        ITemplateService templateService)
        : base("Matches", "Open a match or set up a new analysis.")
    {
        _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));

        OpenCreateDialogCommand = new RelayCommand(_ => OpenCreateDialog(), _ => !IsBusy);
        CreateMatchCommand = new AsyncCommand(CreateMatchAsync, CanCreateMatch);
        DiscardCreateDialogCommand = new RelayCommand(_ => CloseCreateDialog(), _ => !IsBusy);
        RequestDeleteMatchCommand = new RelayCommand(RequestDeleteMatch, parameter => !IsBusy && parameter is Match);
        ConfirmDeleteMatchCommand = new AsyncCommand(DeleteMatchAsync, () => !IsBusy && IsDeleteDialogOpen && PendingDeleteMatch is not null);
        CancelDeleteMatchCommand = new RelayCommand(_ => CloseDeleteDialog(), _ => !IsBusy);
        RefreshCommand = new AsyncCommand(InitializeAsync, () => !IsBusy);
        ClearVideoCommand = new RelayCommand(_ => VideoPath = null, _ => !IsBusy && !string.IsNullOrWhiteSpace(VideoPath));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Match> Matches { get; } = [];
    public ObservableCollection<Team> Teams { get; } = [];
    public ObservableCollection<Template> Templates { get; } = [];

    public ICommand OpenCreateDialogCommand { get; }
    public ICommand CreateMatchCommand { get; }
    public ICommand DiscardCreateDialogCommand { get; }
    public ICommand RequestDeleteMatchCommand { get; }
    public ICommand ConfirmDeleteMatchCommand { get; }
    public ICommand CancelDeleteMatchCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearVideoCommand { get; }

    public Team? SelectedTeam
    {
        get => _selectedTeam;
        set
        {
            if (SetProperty(ref _selectedTeam, value))
                NotifyCommandStates();
        }
    }

    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetProperty(ref _selectedTemplate, value))
                NotifyCommandStates();
        }
    }

    public string? VideoPath
    {
        get => _videoPath;
        set
        {
            if (SetProperty(ref _videoPath, value))
            {
                OnPropertyChanged(nameof(HasVideo));
                OnPropertyChanged(nameof(VideoFileName));
                NotifyCommandStates();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                NotifyCommandStates();
            }
        }
    }

    public bool IsCreateDialogOpen
    {
        get => _isCreateDialogOpen;
        private set
        {
            if (SetProperty(ref _isCreateDialogOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                NotifyCommandStates();
            }
        }
    }

    public bool IsDeleteDialogOpen
    {
        get => _isDeleteDialogOpen;
        private set
        {
            if (SetProperty(ref _isDeleteDialogOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                NotifyCommandStates();
            }
        }
    }

    public Match? PendingDeleteMatch
    {
        get => _pendingDeleteMatch;
        private set
        {
            if (SetProperty(ref _pendingDeleteMatch, value))
                NotifyCommandStates();
        }
    }

    public bool IsAnyDialogOpen => IsCreateDialogOpen || IsDeleteDialogOpen;
    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasMatches => Matches.Count > 0;
    public bool HasNoMatches => Matches.Count == 0;
    public bool HasTeams => Teams.Count > 0;
    public bool HasNoTeams => Teams.Count == 0;
    public bool HasTemplates => Templates.Count > 0;
    public bool HasNoTemplates => Templates.Count == 0;
    public bool HasVideo => !string.IsNullOrWhiteSpace(VideoPath);
    public string VideoFileName =>
        string.IsNullOrWhiteSpace(VideoPath)
            ? "No video selected (optional)"
            : Path.GetFileName(VideoPath) ?? "No video selected (optional)";
    public int MatchCount => Matches.Count;
    public int TeamCount => Teams.Count;
    public int TemplateCount => Templates.Count;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var selectedTeamId = SelectedTeam?.Id;
            var selectedTemplateId = SelectedTemplate?.Id;

            var teams = await _teamService.GetAllAsync();
            Teams.Clear();
            foreach (var team in teams)
                Teams.Add(team);
            SelectedTeam = Teams.FirstOrDefault(team => team.Id == selectedTeamId) ?? Teams.FirstOrDefault();

            var templates = await _templateService.GetAllAsync();
            Templates.Clear();
            foreach (var template in templates)
                Templates.Add(template);
            SelectedTemplate = Templates.FirstOrDefault(template => template.Id == selectedTemplateId)
                ?? Templates.FirstOrDefault();

            await LoadMatchesCoreAsync();
            NotifyLookupDataChanged();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not load matches: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SetVideoPath(string? path)
    {
        VideoPath = path;
        ClearError();
    }

    public void ReportVideoPickerError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ErrorMessage = $"Could not select video: {exception.Message}";
    }

    private async Task LoadMatchesCoreAsync()
    {
        var matches = await _matchService.GetAllAsync();
        Matches.Clear();
        foreach (var match in matches)
            Matches.Add(match);
        OnPropertyChanged(nameof(HasMatches));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(MatchCount));
    }

    private void OpenCreateDialog()
    {
        VideoPath = null;
        ClearError();
        IsCreateDialogOpen = true;
    }

    private void CloseCreateDialog()
    {
        IsCreateDialogOpen = false;
        VideoPath = null;
        ClearError();
    }

    private void RequestDeleteMatch(object? parameter)
    {
        if (parameter is not Match match)
            return;

        PendingDeleteMatch = match;
        ClearError();
        IsDeleteDialogOpen = true;
    }

    private void CloseDeleteDialog()
    {
        IsDeleteDialogOpen = false;
        PendingDeleteMatch = null;
        ClearError();
    }

    private async Task DeleteMatchAsync()
    {
        var match = PendingDeleteMatch;
        if (match is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            await _matchService.RemoveAsync(match.Id);
            Matches.Remove(match);
            OnPropertyChanged(nameof(HasMatches));
            OnPropertyChanged(nameof(HasNoMatches));
            OnPropertyChanged(nameof(MatchCount));
            CloseDeleteDialog();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not delete match: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateMatchAsync()
    {
        var team = SelectedTeam;
        var template = SelectedTemplate;
        if (team is null || template is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            await _matchService.CreateAsync(new Match(team.Id, template.Id, VideoPath));
            await LoadMatchesCoreAsync();
            CloseCreateDialog();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not create match: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreateMatch() =>
        !IsBusy && IsCreateDialogOpen && SelectedTeam is not null && SelectedTemplate is not null;

    private void NotifyLookupDataChanged()
    {
        OnPropertyChanged(nameof(HasTeams));
        OnPropertyChanged(nameof(HasNoTeams));
        OnPropertyChanged(nameof(HasTemplates));
        OnPropertyChanged(nameof(HasNoTemplates));
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        foreach (var command in new ICommand[]
        {
            OpenCreateDialogCommand, CreateMatchCommand, DiscardCreateDialogCommand,
            RequestDeleteMatchCommand, ConfirmDeleteMatchCommand, CancelDeleteMatchCommand,
            RefreshCommand, ClearVideoCommand
        })
        {
            if (command is AsyncCommand asyncCommand)
                asyncCommand.NotifyCanExecuteChanged();
            else if (command is RelayCommand relayCommand)
                relayCommand.NotifyCanExecuteChanged();
        }
    }

    private void ClearError() => ErrorMessage = null;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute();

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                await execute();
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class RelayCommand(Action<object?> execute, Func<object?, bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute(parameter);

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                execute(parameter);
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
