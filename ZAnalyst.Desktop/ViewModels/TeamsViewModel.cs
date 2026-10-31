using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Threading;
using ZAnalyst.Application.Exceptions;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class TeamsViewModel : WorkspacePageViewModel, INotifyPropertyChanged
{
    private readonly ITeamService _teamService;
    private readonly IPlayerService _playerService;
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private Team? _selectedTeam;
    private Player? _selectedPlayer;
    private string _newTeamName = string.Empty;
    private string _editTeamName = string.Empty;
    private string _newPlayerName = string.Empty;
    private string _newPlayerNumber = string.Empty;
    private string _editPlayerName = string.Empty;
    private string _editPlayerNumber = string.Empty;
    private string _playerSearchText = string.Empty;
    private string? _errorMessage;
    private string? _toastMessage;
    private string? _toastTitle;
    private bool _isBusy = true;
    private bool _isReloadingTeams;
    private bool _isTeamDialogOpen;
    private bool _isEditingTeamDialog;
    private bool _isDeleteTeamConfirmationOpen;
    private bool _isPlayerDialogOpen;
    private bool _isEditingPlayerDialog;
    private bool _isDeletePlayerConfirmationOpen;

    public TeamsViewModel(ITeamService teamService, IPlayerService playerService)
        : base("Teams & players", "Organize squads and the players available for analysis.")
    {
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            ToastMessage = null;
        };

        OpenCreateTeamCommand = new RelayCommand(_ => OpenTeamDialog(), _ => !IsBusy);
        OpenEditTeamCommand = new RelayCommand(_ => OpenEditTeamDialog(), _ => !IsBusy && SelectedTeam is not null);
        EditTeamCommand = new RelayCommand(_ => OpenEditTeamDialog(), _ => !IsBusy && SelectedTeam is not null);
        CreateTeamCommand = new AsyncCommand(CreateTeamAsync, CanCreateTeam);
        UpdateTeamCommand = new AsyncCommand(UpdateTeamAsync, CanUpdateTeam);
        DeleteTeamCommand = new RelayCommand(_ => IsDeleteTeamConfirmationOpen = true, _ => !IsBusy && SelectedTeam is not null);
        ConfirmDeleteTeamCommand = new AsyncCommand(DeleteTeamAsync, _ => !IsBusy && IsDeleteTeamConfirmationOpen && SelectedTeam is not null);
        OpenCreatePlayerCommand = new RelayCommand(_ => OpenPlayerDialog(), _ => !IsBusy && SelectedTeam is not null);
        CreatePlayerCommand = new AsyncCommand(CreatePlayerAsync, CanCreatePlayer);
        UpdatePlayerCommand = new AsyncCommand(UpdatePlayerAsync, CanUpdatePlayer);
        DeletePlayerCommand = new RelayCommand(_ => IsDeletePlayerConfirmationOpen = true, _ => !IsBusy && SelectedPlayer is not null);
        ConfirmDeletePlayerCommand = new AsyncCommand(DeletePlayerAsync, _ => !IsBusy && IsDeletePlayerConfirmationOpen && SelectedPlayer is not null);
        EditPlayerCommand = new RelayCommand(BeginPlayerEdit, parameter => !IsBusy && parameter is Player);
        CancelPlayerEditCommand = new RelayCommand(ClosePlayerDialog, () => !IsBusy);
        SaveTeamDialogCommand = new AsyncCommand(SaveTeamDialogAsync, CanSaveTeamDialog);
        SavePlayerDialogCommand = new AsyncCommand(SavePlayerDialogAsync, CanSavePlayerDialog);
        DiscardTeamDialogCommand = new RelayCommand(_ => CloseTeamDialog(), _ => !IsBusy);
        DiscardPlayerDialogCommand = new RelayCommand(_ => ClosePlayerDialog(), _ => !IsBusy);
        CancelTeamDeleteCommand = new RelayCommand(_ => IsDeleteTeamConfirmationOpen = false, _ => !IsBusy);
        CancelPlayerDeleteCommand = new RelayCommand(_ => IsDeletePlayerConfirmationOpen = false, _ => !IsBusy);
        RefreshCommand = new AsyncCommand(LoadTeamsAsync, () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Team> Teams { get; } = [];
    public ObservableCollection<Player> Players { get; } = [];
    public ObservableCollection<Player> VisiblePlayers { get; } = [];
    public ICommand CreateTeamCommand { get; }
    public ICommand OpenCreateTeamCommand { get; }
    public ICommand OpenEditTeamCommand { get; }
    public ICommand UpdateTeamCommand { get; }
    public ICommand DeleteTeamCommand { get; }
    public ICommand ConfirmDeleteTeamCommand { get; }
    public ICommand EditTeamCommand { get; }
    public ICommand SaveTeamDialogCommand { get; }
    public ICommand DiscardTeamDialogCommand { get; }
    public ICommand CancelTeamDeleteCommand { get; }
    public ICommand OpenCreatePlayerCommand { get; }
    public ICommand CreatePlayerCommand { get; }
    public ICommand UpdatePlayerCommand { get; }
    public ICommand DeletePlayerCommand { get; }
    public ICommand ConfirmDeletePlayerCommand { get; }
    public ICommand EditPlayerCommand { get; }
    public ICommand CancelPlayerEditCommand { get; }
    public ICommand SavePlayerDialogCommand { get; }
    public ICommand DiscardPlayerDialogCommand { get; }
    public ICommand CancelPlayerDeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public bool IsTeamDialogOpen
    {
        get => _isTeamDialogOpen;
        private set
        {
            if (SetProperty(ref _isTeamDialogOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                OnPropertyChanged(nameof(IsTeamEditorVisible));
                NotifyCommandStates();
            }
        }
    }

    public bool IsEditingTeamDialog
    {
        get => _isEditingTeamDialog;
        private set
        {
            if (SetProperty(ref _isEditingTeamDialog, value))
            {
                OnPropertyChanged(nameof(IsCreatingTeamDialog));
                OnPropertyChanged(nameof(TeamDialogTitle));
                NotifyCommandStates();
            }
        }
    }

    public bool IsCreatingTeamDialog => !IsEditingTeamDialog;
    public string TeamDialogTitle => IsEditingTeamDialog ? "Edit team" : "New team";

    public bool IsDeleteTeamConfirmationOpen
    {
        get => _isDeleteTeamConfirmationOpen;
        set
        {
            if (SetProperty(ref _isDeleteTeamConfirmationOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                OnPropertyChanged(nameof(IsTeamEditorVisible));
                NotifyCommandStates();
            }
        }
    }

    public bool IsPlayerDialogOpen
    {
        get => _isPlayerDialogOpen;
        private set
        {
            if (SetProperty(ref _isPlayerDialogOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                OnPropertyChanged(nameof(IsPlayerEditorVisible));
                NotifyCommandStates();
            }
        }
    }

    public bool IsEditingPlayerDialog
    {
        get => _isEditingPlayerDialog;
        private set
        {
            if (SetProperty(ref _isEditingPlayerDialog, value))
            {
                OnPropertyChanged(nameof(IsCreatingPlayerDialog));
                OnPropertyChanged(nameof(PlayerDialogTitle));
                NotifyCommandStates();
            }
        }
    }

    public bool IsCreatingPlayerDialog => !IsEditingPlayerDialog;
    public string PlayerDialogTitle => IsEditingPlayerDialog ? "Edit player" : "New player";

    public bool IsDeletePlayerConfirmationOpen
    {
        get => _isDeletePlayerConfirmationOpen;
        set
        {
            if (SetProperty(ref _isDeletePlayerConfirmationOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                OnPropertyChanged(nameof(IsPlayerEditorVisible));
                NotifyCommandStates();
            }
        }
    }

    public bool IsAnyDialogOpen =>
        IsTeamDialogOpen || IsDeleteTeamConfirmationOpen || IsPlayerDialogOpen || IsDeletePlayerConfirmationOpen;
    public bool IsTeamEditorVisible => IsTeamDialogOpen && !IsDeleteTeamConfirmationOpen;
    public bool IsPlayerEditorVisible => IsPlayerDialogOpen && !IsDeletePlayerConfirmationOpen;

    public Team? SelectedTeam
    {
        get => _selectedTeam;
        set
        {
            if (ReferenceEquals(_selectedTeam, value))
                return;

            _selectedTeam = value;
            EditTeamName = value?.Name ?? string.Empty;
            SelectedPlayer = null;
            Players.Clear();
            NotifyPlayersChanged();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedTeam));
            OnPropertyChanged(nameof(IsTeamSelected));
            OnPropertyChanged(nameof(RosterTitle));
            OnPropertyChanged(nameof(EmptyRosterMessage));
            ClearError();

            if (value is not null && !_isReloadingTeams)
                _ = LoadPlayersAsync(value);

            NotifyCommandStates();
        }
    }

    public Player? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (ReferenceEquals(_selectedPlayer, value))
                return;

            _selectedPlayer = value;
            EditPlayerName = value?.Name ?? string.Empty;
            EditPlayerNumber = value?.Number.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedPlayer));
            OnPropertyChanged(nameof(HasNoSelectedPlayer));
            OnPropertyChanged(nameof(PlayerDialogTitle));
            NotifyCommandStates();
        }
    }

    public string NewTeamName
    {
        get => _newTeamName;
        set
        {
            if (SetProperty(ref _newTeamName, value))
                NotifyCommandStates();
        }
    }

    public string EditTeamName
    {
        get => _editTeamName;
        set
        {
            if (SetProperty(ref _editTeamName, value))
                NotifyCommandStates();
        }
    }

    public string NewPlayerName
    {
        get => _newPlayerName;
        set
        {
            if (SetProperty(ref _newPlayerName, value))
                NotifyCommandStates();
        }
    }

    public string NewPlayerNumber
    {
        get => _newPlayerNumber;
        set
        {
            if (SetProperty(ref _newPlayerNumber, value))
                NotifyCommandStates();
        }
    }

    public string EditPlayerName
    {
        get => _editPlayerName;
        set
        {
            if (SetProperty(ref _editPlayerName, value))
                NotifyCommandStates();
        }
    }

    public string EditPlayerNumber
    {
        get => _editPlayerNumber;
        set
        {
            if (SetProperty(ref _editPlayerNumber, value))
                NotifyCommandStates();
        }
    }

    public string PlayerSearchText
    {
        get => _playerSearchText;
        set
        {
            if (SetProperty(ref _playerSearchText, value))
                RefreshVisiblePlayers();
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

    public string? ToastMessage
    {
        get => _toastMessage;
        private set
        {
            if (SetProperty(ref _toastMessage, value))
                OnPropertyChanged(nameof(HasToast));
        }
    }

    public string? ToastTitle
    {
        get => _toastTitle;
        private set => SetProperty(ref _toastTitle, value);
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

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasToast => !string.IsNullOrWhiteSpace(ToastMessage);
    public bool HasSelectedTeam => SelectedTeam is not null;
    public bool IsTeamSelected => SelectedTeam is not null;
    public bool HasSelectedPlayer => SelectedPlayer is not null;
    public bool HasNoSelectedPlayer => SelectedPlayer is null;
    public bool HasTeams => Teams.Count > 0;
    public bool HasNoTeams => Teams.Count == 0;
    public bool HasPlayers => Players.Count > 0;
    public bool HasNoPlayers => Players.Count == 0;
    public bool HasNoMatchingPlayers => Players.Count > 0 && VisiblePlayers.Count == 0;
    public string RosterTitle => SelectedTeam is null ? "PLAYER ROSTER" : $"{SelectedTeam.Name.ToUpperInvariant()} ROSTER";
    public string EmptyRosterMessage => SelectedTeam is null
        ? "Select a team to view its roster"
        : "No players on this team yet";

    public Task InitializeAsync() => LoadTeamsCoreAsync();

    public Task LoadTeamsAsync() =>
        IsBusy ? Task.CompletedTask : LoadTeamsCoreAsync();

    private async Task LoadTeamsCoreAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var selectedId = SelectedTeam?.Id;
            var teams = await _teamService.GetAllAsync();
            Teams.Clear();
            foreach (var team in teams)
                Teams.Add(team);
            NotifyTeamsChanged();

            _isReloadingTeams = true;
            SelectedTeam = Teams.FirstOrDefault(team => team.Id == selectedId) ?? Teams.FirstOrDefault();
            _isReloadingTeams = false;
            if (SelectedTeam is not null)
                await LoadPlayersAsync(SelectedTeam);
        }
        catch (Exception exception)
        {
            _isReloadingTeams = false;
            ErrorMessage = $"Could not load teams: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateTeamAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var team = await _teamService.CreateAsync(new Team(NewTeamName));
            Teams.Add(team);
            NotifyTeamsChanged();
            NewTeamName = string.Empty;
            _isReloadingTeams = true;
            SelectedTeam = team;
            _isReloadingTeams = false;
            await LoadPlayersAsync(team);
        }
        catch (Exception exception)
        {
            _isReloadingTeams = false;
            ErrorMessage = $"Could not create team: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateTeamAsync()
    {
        var team = SelectedTeam;
        if (team is null)
            return;

        IsBusy = true;
        ClearError();
        var oldName = team.Name;
        try
        {
            team.UpdateName(EditTeamName);
            await _teamService.UpdateAsync(team.Id, team);
            RefreshCollectionItem(Teams, team);
            EditTeamName = team.Name;
            OnPropertyChanged(nameof(RosterTitle));
        }
        catch (Exception exception)
        {
            team.UpdateName(oldName);
            RefreshCollectionItem(Teams, team);
            EditTeamName = oldName;
            OnPropertyChanged(nameof(RosterTitle));
            ErrorMessage = $"Could not update team: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteTeamAsync(object? parameter = null)
    {
        var team = parameter as Team ?? SelectedTeam;
        if (team is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            await _teamService.RemoveAsync(team.Id);
            Teams.Remove(team);
            NotifyTeamsChanged();
            IsDeleteTeamConfirmationOpen = false;
            var wasSelected = SelectedTeam?.Id == team.Id;
            if (!wasSelected)
                return;

            _isReloadingTeams = true;
            SelectedTeam = Teams.FirstOrDefault();
            _isReloadingTeams = false;
            if (SelectedTeam is not null)
                await LoadPlayersAsync(SelectedTeam);
            else
            {
                Players.Clear();
                VisiblePlayers.Clear();
                NotifyPlayersChanged();
            }
        }
        catch (TeamHasPlayersOrMatchesException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not delete team: {exception.Message}";
        }
        finally
        {
            _isReloadingTeams = false;
            IsBusy = false;
        }
    }

    private async Task CreatePlayerAsync()
    {
        var team = SelectedTeam;
        if (team is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            var number = ParsePlayerNumber(NewPlayerNumber);
            var player = await _playerService.CreateAsync(new Player(NewPlayerName, number, team.Id));
            InsertPlayerInNumberOrder(player);
            NewPlayerName = string.Empty;
            NewPlayerNumber = string.Empty;
            NotifyPlayersChanged();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not add player: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdatePlayerAsync()
    {
        var player = SelectedPlayer;
        var team = SelectedTeam;
        if (player is null || team is null)
            return;

        IsBusy = true;
        ClearError();
        var oldName = player.Name;
        var oldNumber = player.Number;
        try
        {
            player.Update(EditPlayerName, ParsePlayerNumber(EditPlayerNumber), team.Id);
            await _playerService.UpdateAsync(player.Id, player);
            Players.Remove(player);
            InsertPlayerInNumberOrder(player);
            NotifyPlayersChanged();
        }
        catch (Exception exception)
        {
            player.Update(oldName, oldNumber, team.Id);
            RefreshCollectionItem(Players, player);
            EditPlayerName = oldName;
            EditPlayerNumber = oldNumber.ToString(CultureInfo.InvariantCulture);
            ErrorMessage = $"Could not update player: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeletePlayerAsync(object? parameter = null)
    {
        var player = parameter as Player ?? SelectedPlayer;
        if (player is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            await _playerService.RemoveAsync(player.Id);
            Players.Remove(player);
            SelectedPlayer = null;
            IsDeletePlayerConfirmationOpen = false;
            IsPlayerDialogOpen = false;
            NotifyPlayersChanged();
        }
        catch (PlayerHasRecordedActionsException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not delete player: {exception.Message}";
        }
        finally
        {
            RefreshVisiblePlayers();
            IsBusy = false;
        }
    }

    private async Task LoadPlayersAsync(Team team)
    {
        var wasBusy = IsBusy;
        if (!wasBusy)
            IsBusy = true;
        try
        {
            await ReloadSelectedPlayersAsync(team.Id);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not load players: {exception.Message}";
        }
        finally
        {
            if (!wasBusy)
                IsBusy = false;
        }
    }

    private async Task ReloadSelectedPlayersAsync(Guid teamId)
    {
        var team = await _teamService.GetByIdWithPlayersAsync(teamId);
        if (SelectedTeam?.Id != teamId)
            return;

        Players.Clear();
        if (team is not null)
        {
            foreach (var player in team.Players.OrderBy(player => player.Number))
                Players.Add(player);
        }
        RefreshVisiblePlayers();
        NotifyPlayersChanged();
    }

    private void OpenTeamDialog()
    {
        IsEditingTeamDialog = false;
        NewTeamName = string.Empty;
        ClearError();
        IsTeamDialogOpen = true;
    }

    private void OpenEditTeamDialog()
    {
        if (SelectedTeam is not { } team)
            return;

        EditTeamName = team.Name;
        IsEditingTeamDialog = true;
        ClearError();
        IsTeamDialogOpen = true;
    }

    private void CloseTeamDialog()
    {
        IsTeamDialogOpen = false;
        IsEditingTeamDialog = false;
        NewTeamName = string.Empty;
        EditTeamName = SelectedTeam?.Name ?? string.Empty;
    }

    private async Task SaveTeamDialogAsync()
    {
        if (IsEditingTeamDialog)
            await UpdateTeamAsync();
        else
            await CreateTeamAsync();

        if (!HasError)
            CloseTeamDialog();
    }

    private bool CanSaveTeamDialog() =>
        !IsBusy
        && IsTeamDialogOpen
        && (IsEditingTeamDialog
            ? SelectedTeam is not null && !string.IsNullOrWhiteSpace(EditTeamName)
            : !string.IsNullOrWhiteSpace(NewTeamName));

    private void OpenPlayerDialog()
    {
        SelectedPlayer = null;
        NewPlayerName = string.Empty;
        NewPlayerNumber = string.Empty;
        ClearError();
        IsEditingPlayerDialog = false;
        IsPlayerDialogOpen = true;
    }

    private void ClosePlayerDialog()
    {
        IsPlayerDialogOpen = false;
        IsEditingPlayerDialog = false;
        IsDeletePlayerConfirmationOpen = false;
        SelectedPlayer = null;
        NewPlayerName = string.Empty;
        NewPlayerNumber = string.Empty;
        EditPlayerName = string.Empty;
        EditPlayerNumber = string.Empty;
        ClearError();
    }

    private async Task SavePlayerDialogAsync()
    {
        if (IsEditingPlayerDialog)
            await UpdatePlayerAsync();
        else
            await CreatePlayerAsync();

        if (!HasError)
            ClosePlayerDialog();
    }

    private bool CanSavePlayerDialog() =>
        !IsBusy
        && IsPlayerDialogOpen
        && SelectedTeam is not null
        && (IsEditingPlayerDialog
            ? SelectedPlayer is not null
                && !string.IsNullOrWhiteSpace(EditPlayerName)
                && !string.IsNullOrWhiteSpace(EditPlayerNumber)
            : SelectedPlayer is null
                && !string.IsNullOrWhiteSpace(NewPlayerName)
                && !string.IsNullOrWhiteSpace(NewPlayerNumber));

    private bool CanCreateTeam() => !IsBusy && !string.IsNullOrWhiteSpace(NewTeamName);
    private bool CanUpdateTeam() => !IsBusy && SelectedTeam is not null && !string.IsNullOrWhiteSpace(EditTeamName);
    private bool CanCreatePlayer() =>
        !IsBusy
        && SelectedTeam is not null
        && SelectedPlayer is null
        && !string.IsNullOrWhiteSpace(NewPlayerName)
        && !string.IsNullOrWhiteSpace(NewPlayerNumber);
    private bool CanUpdatePlayer() =>
        !IsBusy && SelectedPlayer is not null
        && !string.IsNullOrWhiteSpace(EditPlayerName)
        && !string.IsNullOrWhiteSpace(EditPlayerNumber);
    private void BeginTeamEdit(object? parameter)
    {
        if (parameter is Team team)
            SelectedTeam = team;
    }

    private void BeginPlayerEdit(object? parameter)
    {
        if (parameter is Player player)
        {
            SelectedPlayer = player;
            EditPlayerName = player.Name;
            EditPlayerNumber = player.Number.ToString(CultureInfo.InvariantCulture);
            IsEditingPlayerDialog = true;
            ClearError();
            IsPlayerDialogOpen = true;
        }
    }

    private static int ParsePlayerNumber(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            throw new ArgumentException("Enter a valid squad number.");
        return number;
    }

    private static void RefreshCollectionItem<T>(ObservableCollection<T> collection, T item)
    {
        var index = collection.IndexOf(item);
        if (index >= 0)
        {
            collection.RemoveAt(index);
            collection.Insert(index, item);
        }
    }

    private void InsertPlayerInNumberOrder(Player player)
    {
        var index = 0;
        while (index < Players.Count && Players[index].Number <= player.Number)
            index++;
        Players.Insert(index, player);
        RefreshVisiblePlayers();
    }

    private void RefreshVisiblePlayers()
    {
        var search = PlayerSearchText.Trim();
        var visible = Players
            .Where(player => search.Length == 0
                || player.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || player.Number.ToString(CultureInfo.InvariantCulture).Contains(search, StringComparison.Ordinal))
            .ToArray();

        VisiblePlayers.Clear();
        foreach (var player in visible)
            VisiblePlayers.Add(player);

        if (SelectedPlayer is not null && !visible.Contains(SelectedPlayer))
            SelectedPlayer = null;

        OnPropertyChanged(nameof(HasNoMatchingPlayers));
    }

    private void ShowToast(string title, string message)
    {
        ToastTitle = title;
        ToastMessage = message;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void NotifyTeamsChanged()
    {
        OnPropertyChanged(nameof(HasTeams));
        OnPropertyChanged(nameof(HasNoTeams));
    }

    private void NotifyPlayersChanged()
    {
        OnPropertyChanged(nameof(HasPlayers));
        OnPropertyChanged(nameof(HasNoPlayers));
        OnPropertyChanged(nameof(HasNoMatchingPlayers));
    }

    private void NotifyCommandStates()
    {
        foreach (var command in new ICommand[]
        {
            CreateTeamCommand, OpenCreateTeamCommand, OpenEditTeamCommand, UpdateTeamCommand,
            DeleteTeamCommand, ConfirmDeleteTeamCommand, EditTeamCommand, SaveTeamDialogCommand,
            DiscardTeamDialogCommand, CancelTeamDeleteCommand, OpenCreatePlayerCommand,
            CreatePlayerCommand, UpdatePlayerCommand, DeletePlayerCommand, ConfirmDeletePlayerCommand,
            EditPlayerCommand, CancelPlayerEditCommand, SavePlayerDialogCommand,
            DiscardPlayerDialogCommand, CancelPlayerDeleteCommand, RefreshCommand
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

    private sealed class AsyncCommand(Func<object?, Task> execute, Func<object?, bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute(parameter);

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                await execute(parameter);
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public AsyncCommand(Func<Task> execute, Func<bool> canExecute)
            : this(_ => execute(), _ => canExecute())
        {
        }
    }

    private sealed class RelayCommand(System.Action<object?> execute, Func<object?, bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute(parameter);

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                execute(parameter);
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public RelayCommand(System.Action execute, Func<bool> canExecute)
            : this(_ => execute(), _ => canExecute())
        {
        }
    }
}
