using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Domain.Enums;
using Action = ZAnalyst.Domain.Entities.Action;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class MatchAnalysisViewModel : WorkspacePageViewModel, INotifyPropertyChanged
{
    public const double ManualTimelineDurationSeconds = 10800;

    private readonly IEventService _eventService;
    private readonly IActionService _actionService;
    private readonly IButtonService _buttonService;
    private readonly ITeamService _teamService;
    private Match? _selectedMatch;
    private Player? _selectedPlayer;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isPlaying;
    private double _playbackPositionSeconds;
    private double _playbackDurationSeconds;
    private int _selectedTabIndex;
    private Button? _pendingDoublePressButton;
    private double? _pendingDoublePressStart;
    private AnalysisEntryViewModel? _editingEntry;
    private string _editStartTime = string.Empty;
    private string _editEndTime = string.Empty;
    private string _editNote = string.Empty;
    private Player? _editPlayer;
    private ActionRate _editRate = ActionRate.Natural;
    private bool _isEditDialogOpen;
    private bool _isDeleteConfirmationOpen;

    public MatchAnalysisViewModel(
        IEventService eventService,
        IActionService actionService,
        IButtonService buttonService,
        ITeamService teamService)
        : base("Match analysis", "Record match events and player actions.")
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
        _buttonService = buttonService ?? throw new ArgumentNullException(nameof(buttonService));
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));

        RecordButtonCommand = new AsyncCommand(RecordButtonAsync, parameter => !IsBusy && parameter is Button);
        EditEntryCommand = new RelayCommand(parameter => OpenEditDialog(parameter as AnalysisEntryViewModel),
            parameter => !IsBusy && parameter is AnalysisEntryViewModel);
        SaveEntryCommand = new AsyncCommand(SaveEntryAsync, () => !IsBusy && IsEditDialogOpen);
        DiscardEditCommand = new RelayCommand(_ => CloseEditDialog(), _ => !IsBusy);
        RequestDeleteEntryCommand = new RelayCommand(_ => IsDeleteConfirmationOpen = true,
            _ => !IsBusy && EditingEntry is not null);
        ConfirmDeleteEntryCommand = new AsyncCommand(DeleteEntryAsync,
            () => !IsBusy && IsDeleteConfirmationOpen && EditingEntry is not null);
        CancelDeleteEntryCommand = new RelayCommand(_ => IsDeleteConfirmationOpen = false, _ => !IsBusy);
        SeekToEntryCommand = new RelayCommand(parameter =>
        {
            if (parameter is AnalysisEntryViewModel entry)
                PlaybackPositionSeconds = entry.StartSeconds;
        }, parameter => parameter is AnalysisEntryViewModel);
        TogglePlaybackCommand = new RelayCommand(_ => TogglePlaybackRequested?.Invoke(), _ => HasVideo);
        BackCommand = new RelayCommand(_ => BackRequested?.Invoke(), _ => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event System.Action<double>? SeekRequested;
    public event System.Action? TogglePlaybackRequested;
    public event System.Action? BackRequested;

    public ObservableCollection<Button> EventButtons { get; } = [];
    public ObservableCollection<Button> ActionButtons { get; } = [];
    public ObservableCollection<AnalysisEntryViewModel> EventEntries { get; } = [];
    public ObservableCollection<AnalysisEntryViewModel> ActionEntries { get; } = [];
    public ObservableCollection<Player> Players { get; } = [];
    public IReadOnlyList<ActionRate> ActionRates { get; } = Enum.GetValues<ActionRate>();

    public ICommand RecordButtonCommand { get; }
    public ICommand EditEntryCommand { get; }
    public ICommand SaveEntryCommand { get; }
    public ICommand DiscardEditCommand { get; }
    public ICommand RequestDeleteEntryCommand { get; }
    public ICommand ConfirmDeleteEntryCommand { get; }
    public ICommand CancelDeleteEntryCommand { get; }
    public ICommand SeekToEntryCommand { get; }
    public ICommand TogglePlaybackCommand { get; }
    public ICommand BackCommand { get; }

    public Match? SelectedMatch
    {
        get => _selectedMatch;
        private set
        {
            if (SetProperty(ref _selectedMatch, value))
            {
                OnPropertyChanged(nameof(MatchTitle));
                OnPropertyChanged(nameof(VideoPath));
                OnPropertyChanged(nameof(HasVideo));
                OnPropertyChanged(nameof(HasMatch));
                NotifyCommands();
            }
        }
    }

    public Player? SelectedPlayer
    {
        get => _selectedPlayer;
        set => SetProperty(ref _selectedPlayer, value);
    }

    public AnalysisEntryViewModel? EditingEntry
    {
        get => _editingEntry;
        private set
        {
            if (SetProperty(ref _editingEntry, value))
                NotifyCommands();
        }
    }

    public string EditStartTime
    {
        get => _editStartTime;
        set => SetProperty(ref _editStartTime, value);
    }

    public string EditEndTime
    {
        get => _editEndTime;
        set => SetProperty(ref _editEndTime, value);
    }

    public string EditNote
    {
        get => _editNote;
        set => SetProperty(ref _editNote, value);
    }

    public Player? EditPlayer
    {
        get => _editPlayer;
        set => SetProperty(ref _editPlayer, value);
    }

    public ActionRate EditRate
    {
        get => _editRate;
        set => SetProperty(ref _editRate, value);
    }

    public bool IsEditDialogOpen
    {
        get => _isEditDialogOpen;
        private set
        {
            if (SetProperty(ref _isEditDialogOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                NotifyCommands();
            }
        }
    }

    public bool IsDeleteConfirmationOpen
    {
        get => _isDeleteConfirmationOpen;
        private set
        {
            if (SetProperty(ref _isDeleteConfirmationOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                NotifyCommands();
            }
        }
    }

    public bool IsAnyDialogOpen => IsEditDialogOpen || IsDeleteConfirmationOpen;
    public bool HasMatch => SelectedMatch is not null;
    public string MatchTitle => SelectedMatch?.Title ?? "Select a match";
    public string? VideoPath => SelectedMatch?.VideoPath;
    public bool HasVideo => !string.IsNullOrWhiteSpace(VideoPath);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasEvents => EventEntries.Count > 0;
    public bool HasActions => ActionEntries.Count > 0;
    public bool HasEventButtons => EventButtons.Count > 0;
    public bool HasActionButtons => ActionButtons.Count > 0;
    public bool HasPlayers => Players.Count > 0;
    public bool HasPendingPress => _pendingDoublePressButton is not null;
    public bool IsActionTab => SelectedTabIndex == 1;
    public bool IsNotBusy => !IsBusy;
    public string CurrentTimeText => FormatClock(PlaybackPositionSeconds);
    public string DurationText => FormatClock(PlaybackDurationSeconds);
    public string PlaybackButtonText => IsPlaying ? "Pause" : "Play";
    public string PendingPressText => _pendingDoublePressButton is null
        ? string.Empty
        : $"First press recorded for {_pendingDoublePressButton.Name}. Press it again to set the end.";
    public double PlaybackPositionSeconds
    {
        get => _playbackPositionSeconds;
        set
        {
            var boundedPosition = Math.Clamp(value, 0, PlaybackDurationSeconds > 0 ? PlaybackDurationSeconds : double.MaxValue);
            if (SetProperty(ref _playbackPositionSeconds, boundedPosition))
            {
                OnPropertyChanged(nameof(CurrentTimeText));
                SeekRequested?.Invoke(boundedPosition);
            }
        }
    }

    public double PlaybackDurationSeconds
    {
        get => _playbackDurationSeconds;
        private set
        {
            if (SetProperty(ref _playbackDurationSeconds, value))
                OnPropertyChanged(nameof(DurationText));
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
                OnPropertyChanged(nameof(IsActionTab));
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetProperty(ref _isPlaying, value))
                OnPropertyChanged(nameof(PlaybackButtonText));
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
                NotifyCommands();
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

    public async Task LoadMatchAsync(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        IsBusy = true;
        ClearError();
        try
        {
            SelectedMatch = match;
            PlaybackPositionSeconds = 0;
            PlaybackDurationSeconds = string.IsNullOrWhiteSpace(match.VideoPath)
                ? ManualTimelineDurationSeconds
                : 0;
            ClearPendingPress();
            EventEntries.Clear();
            ActionEntries.Clear();
            EventButtons.Clear();
            ActionButtons.Clear();
            Players.Clear();

            var buttons = await _buttonService.GetTemplateButtonsAsync(match.TemplateId);
            foreach (var button in buttons.Where(button => button.Type == ButtonType.Event))
                EventButtons.Add(button);
            foreach (var button in buttons.Where(button => button.Type == ButtonType.Action))
                ActionButtons.Add(button);
            OnPropertyChanged(nameof(HasEventButtons));
            OnPropertyChanged(nameof(HasActionButtons));

            var team = await _teamService.GetByIdWithPlayersAsync(match.TeamId)
                ?? throw new InvalidOperationException("The match team could not be found.");
            foreach (var player in team.Players.OrderBy(player => player.Number).ThenBy(player => player.Name))
                Players.Add(player);
            SelectedPlayer = Players.FirstOrDefault();

            var events = await _eventService.GetMatchEventsAsync(match.Id);
            foreach (var domainEvent in events.OrderBy(domainEvent => domainEvent.StartTime))
                EventEntries.Add(AnalysisEntryViewModel.FromEvent(domainEvent, FindButton(domainEvent.ButtonId)));

            var actions = await _actionService.GetMatchActionsAsync(match.Id);
            foreach (var action in actions.OrderBy(action => action.StartTime))
                ActionEntries.Add(AnalysisEntryViewModel.FromAction(
                    action, FindButton(action.ButtonId), Players.FirstOrDefault(player => player.Id == action.PlayerId)));

            OnPropertyChanged(nameof(HasEvents));
            OnPropertyChanged(nameof(HasActions));
            OnPropertyChanged(nameof(HasPlayers));
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not load match analysis: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UpdatePlaybackState(double positionSeconds, double durationSeconds, bool isPlaying)
    {
        PlaybackDurationSeconds = Math.Max(0, durationSeconds);
        _playbackPositionSeconds = Math.Clamp(positionSeconds, 0,
            PlaybackDurationSeconds > 0 ? PlaybackDurationSeconds : double.MaxValue);
        OnPropertyChanged(nameof(PlaybackPositionSeconds));
        OnPropertyChanged(nameof(CurrentTimeText));
        IsPlaying = isPlaying;
    }

    public void ReportVideoError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ErrorMessage = $"Could not play match video: {exception.Message}";
    }

    private async Task RecordButtonAsync(object? parameter)
    {
        if (parameter is not Button button || SelectedMatch is null)
            return;

        ClearError();
        var currentTime = PlaybackPositionSeconds;
        if (button.Mode == PressMode.Double)
        {
            if (_pendingDoublePressButton?.Id != button.Id)
            {
                _pendingDoublePressButton = button;
                _pendingDoublePressStart = currentTime;
                OnPropertyChanged(nameof(PendingPressText));
                OnPropertyChanged(nameof(HasPendingPress));
                return;
            }

            var start = _pendingDoublePressStart ?? currentTime;
            ClearPendingPress();
            if (currentTime < start)
            {
                ErrorMessage = "The second press must be at or after the first press in the video.";
                return;
            }

            await CreateEntryAsync(button, start, currentTime);
            return;
        }

        var singleStart = Math.Max(0, currentTime - button.PreSec);
        var singleEnd = currentTime + button.PostSec;
        if (PlaybackDurationSeconds > 0)
            singleEnd = Math.Min(singleEnd, PlaybackDurationSeconds);
        await CreateEntryAsync(button, singleStart, Math.Max(singleStart, singleEnd));
    }

    private async Task CreateEntryAsync(Button button, double startSeconds, double endSeconds)
    {
        var match = SelectedMatch;
        if (match is null)
            return;
        if (button.Type == ButtonType.Action && SelectedPlayer is null)
        {
            ErrorMessage = "Choose a player before recording an action.";
            return;
        }

        IsBusy = true;
        ClearError();
        try
        {
            var start = ToVideoTime(startSeconds);
            var end = ToVideoTime(endSeconds);
            if (button.Type == ButtonType.Event)
            {
                var domainEvent = await _eventService.CreateAsync(new Event(match.Id, button.Id, start, end, null));
                InsertByStartTime(EventEntries, AnalysisEntryViewModel.FromEvent(domainEvent, button));
                OnPropertyChanged(nameof(HasEvents));
            }
            else
            {
                var action = await _actionService.CreateAsync(new Action(
                    match.Id, button.Id, SelectedPlayer!.Id, ActionRate.Natural, start, end, null));
                InsertByStartTime(ActionEntries, AnalysisEntryViewModel.FromAction(action, button, SelectedPlayer));
                OnPropertyChanged(nameof(HasActions));
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not record entry: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenEditDialog(AnalysisEntryViewModel? entry)
    {
        if (entry is null)
            return;

        EditingEntry = entry;
        EditStartTime = FormatEditableTime(entry.StartSeconds);
        EditEndTime = FormatEditableTime(entry.EndSeconds);
        EditNote = entry.Note ?? string.Empty;
        EditPlayer = entry.Player;
        EditRate = entry.Rate;
        IsDeleteConfirmationOpen = false;
        ClearError();
        IsEditDialogOpen = true;
    }

    private async Task SaveEntryAsync()
    {
        var entry = EditingEntry;
        if (entry is null)
            return;
        if (!TryParseEditableTime(EditStartTime, out var start) ||
            !TryParseEditableTime(EditEndTime, out var end) || start < 0 || end < start)
        {
            ErrorMessage = "Enter valid video times (HH:MM:SS or HH:MM:SS.fff) and ensure the end is not before the start.";
            return;
        }
        if (entry.Action is not null && EditPlayer is null)
        {
            ErrorMessage = "Choose a player for this action.";
            return;
        }

        IsBusy = true;
        ClearError();
        try
        {
            var startTime = ToVideoTime(start);
            var endTime = ToVideoTime(end);
            if (entry.Event is not null)
            {
                entry.Event.UpdateTime(startTime, endTime);
                entry.Event.UpdateNote(EditNote);
                await _eventService.UpdateAsync(entry.Event.Id, entry.Event);
            }
            else if (entry.Action is not null)
            {
                entry.Action.UpdateTime(startTime, endTime);
                entry.Action.UpdateNote(EditNote);
                entry.Action.UpdatePlayer(EditPlayer!.Id);
                entry.Action.UpdateRate(EditRate);
                await _actionService.UpdateAsync(entry.Action.Id, entry.Action);
            }

            if (entry.Event is not null)
            {
                EventEntries.Remove(entry);
                InsertByStartTime(EventEntries, AnalysisEntryViewModel.FromEvent(entry.Event, entry.Button));
            }
            else if (entry.Action is not null)
            {
                ActionEntries.Remove(entry);
                InsertByStartTime(ActionEntries, AnalysisEntryViewModel.FromAction(entry.Action, entry.Button, EditPlayer));
            }
            CloseEditDialog();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not save entry: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteEntryAsync()
    {
        var entry = EditingEntry;
        if (entry is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            if (entry.Event is not null)
            {
                await _eventService.RemoveAsync(entry.Event.Id);
                EventEntries.Remove(entry);
                OnPropertyChanged(nameof(HasEvents));
            }
            else if (entry.Action is not null)
            {
                await _actionService.RemoveAsync(entry.Action.Id);
                ActionEntries.Remove(entry);
                OnPropertyChanged(nameof(HasActions));
            }

            CloseEditDialog();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not delete entry: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Button? FindButton(Guid buttonId) =>
        EventButtons.Concat(ActionButtons).FirstOrDefault(button => button.Id == buttonId);

    private void ClearPendingPress()
    {
        _pendingDoublePressButton = null;
        _pendingDoublePressStart = null;
        OnPropertyChanged(nameof(PendingPressText));
        OnPropertyChanged(nameof(HasPendingPress));
    }

    private void CloseEditDialog()
    {
        IsEditDialogOpen = false;
        IsDeleteConfirmationOpen = false;
        EditingEntry = null;
        ClearError();
    }

    private static void InsertByStartTime(
        ObservableCollection<AnalysisEntryViewModel> entries,
        AnalysisEntryViewModel entry)
    {
        var index = 0;
        while (index < entries.Count && entries[index].StartSeconds <= entry.StartSeconds)
            index++;
        entries.Insert(index, entry);
    }

    private static DateTime ToVideoTime(double seconds) =>
        DateTime.UnixEpoch.AddMilliseconds(Math.Round(seconds * 1000));

    private static double ToSeconds(DateTime time) => (time - DateTime.UnixEpoch).TotalSeconds;

    private static string FormatClock(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatEditableTime(double seconds) =>
        TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

    private static bool TryParseEditableTime(string text, out double seconds)
    {
        if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var value))
        {
            seconds = value.TotalSeconds;
            return true;
        }

        seconds = 0;
        return false;
    }

    private void NotifyCommands()
    {
        foreach (var command in new ICommand[]
        {
            RecordButtonCommand, EditEntryCommand, SaveEntryCommand, DiscardEditCommand,
            RequestDeleteEntryCommand, ConfirmDeleteEntryCommand, CancelDeleteEntryCommand,
            SeekToEntryCommand, TogglePlaybackCommand, BackCommand
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
        public AsyncCommand(Func<Task> execute, Func<bool> canExecute)
            : this(_ => execute(), _ => canExecute())
        {
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute(parameter);

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                await execute(parameter);
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

    public sealed class AnalysisEntryViewModel
    {
        private AnalysisEntryViewModel(Event domainEvent, Button? button)
        {
            Event = domainEvent;
            Button = button;
            StartSeconds = ToSeconds(domainEvent.StartTime);
            EndSeconds = ToSeconds(domainEvent.EndTime);
            Note = domainEvent.Note;
        }

        private AnalysisEntryViewModel(Action action, Button? button, Player? player)
        {
            Action = action;
            Button = button;
            Player = player;
            StartSeconds = ToSeconds(action.StartTime);
            EndSeconds = ToSeconds(action.EndTime);
            Note = action.Note;
            Rate = action.Rate;
        }

        public Event? Event { get; }
        public Action? Action { get; }
        public Button? Button { get; }
        public Player? Player { get; private set; }
        public double StartSeconds { get; private set; }
        public double EndSeconds { get; private set; }
        public string? Note { get; private set; }
        public ActionRate Rate { get; private set; } = ActionRate.Natural;
        public string ButtonName => Button?.Name ?? "Archived button";
        public string NavigationLabel => $"{FormatClock(StartSeconds)} · {ButtonName}";
        public string TimeRange => $"{FormatEditableTime(StartSeconds)} – {FormatEditableTime(EndSeconds)}";
        public string PlayerLabel => Player is null ? "Player not available" : $"#{Player.Number} {Player.Name}";
        public bool IsAction => Action is not null;
        public bool HasNote => !string.IsNullOrWhiteSpace(Note);
        public string? NoteDisplay => HasNote ? Note : "No note";

        public static AnalysisEntryViewModel FromEvent(Event domainEvent, Button? button) =>
            new(domainEvent, button);

        public static AnalysisEntryViewModel FromAction(Action action, Button? button, Player? player) =>
            new(action, button, player);

    }
}
