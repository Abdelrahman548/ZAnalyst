using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Domain.Enums;
using Action = ZAnalyst.Domain.Entities.Action;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class MatchAnalysisViewModel : WorkspacePageViewModel, INotifyPropertyChanged
{
    public const double ManualTimelineDurationSeconds = 10800;
    public const double TimelineTrackWidth = 640;
    private static readonly Geometry PlayGeometry = Geometry.Parse("M6 3L20 12L6 21Z");
    private static readonly Geometry PauseGeometry = Geometry.Parse("M5 4H10V20H5ZM14 4H19V20H14Z");

    private readonly IEventService _eventService;
    private readonly IActionService _actionService;
    private readonly IButtonService _buttonService;
    private readonly ITeamService _teamService;
    private Match? _selectedMatch;
    private Player? _selectedPlayer;
    private PlayerFilterOptionViewModel? _selectedActionPlayerFilter;
    private ButtonFilterOptionViewModel? _selectedActionButtonFilter;
    private ActionRate? _actionRateFilter;
    private AnalysisEntryViewModel? _selectedActionClip;
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
    private bool _isNoteDialogOpen;
    private AnalysisEntryViewModel? _noteEntry;
    private string _noteText = string.Empty;

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
        DeleteEntryCommand = new AsyncCommand(DeleteEntryAsync,
            parameter => !IsBusy && parameter is AnalysisEntryViewModel);
        SeekToEntryCommand = new RelayCommand(parameter =>
        {
            if (parameter is AnalysisEntryViewModel entry)
                PlaybackPositionSeconds = entry.StartSeconds;
        }, parameter => parameter is AnalysisEntryViewModel);
        TogglePlaybackCommand = new RelayCommand(_ => TogglePlaybackRequested?.Invoke(), _ => HasVideo);
        BackCommand = new RelayCommand(_ => BackRequested?.Invoke(), _ => !IsBusy);
        SelectPlayerCommand = new RelayCommand(parameter =>
        {
            if (parameter is PlayerChoiceViewModel choice)
                SelectedPlayer = choice.Player;
        }, parameter => parameter is PlayerChoiceViewModel);
        SelectActionPlayerFilterCommand = new RelayCommand(parameter =>
        {
            if (parameter is PlayerFilterOptionViewModel option)
                SelectedActionPlayerFilter = option;
        }, parameter => parameter is PlayerFilterOptionViewModel);
        SelectActionButtonFilterCommand = new RelayCommand(parameter =>
        {
            if (parameter is ButtonFilterOptionViewModel option)
                SelectedActionButtonFilter = option;
        }, parameter => parameter is ButtonFilterOptionViewModel);
        ToggleActionRateFilterCommand = new RelayCommand(parameter =>
        {
            if (parameter is ActionRate rate)
                ActionRateFilter = ActionRateFilter == rate ? null : rate;
        }, parameter => parameter is ActionRate);
        ClearActionFiltersCommand = new RelayCommand(_ =>
        {
            SelectedActionPlayerFilter = PlayerFilterOptions.FirstOrDefault();
            SelectedActionButtonFilter = ButtonFilterOptions.FirstOrDefault();
            ActionRateFilter = null;
        }, _ => !IsBusy);
        SelectEventsTabCommand = new RelayCommand(_ => SelectedTabIndex = 0, _ => !IsBusy);
        SelectActionsTabCommand = new RelayCommand(_ => SelectedTabIndex = 1, _ => !IsBusy);
        SelectClipsTabCommand = new RelayCommand(_ => SelectedTabIndex = 2, _ => !IsBusy);
        SelectActionClipCommand = new RelayCommand(parameter =>
        {
            if (parameter is AnalysisEntryViewModel entry)
                SelectedActionClip = entry;
        }, parameter => parameter is AnalysisEntryViewModel { IsAction: true });
        SetActionRateCommand = new AsyncCommand(SetActionRateAsync, parameter => !IsBusy && parameter is ActionRateOptionViewModel);
        OpenNoteCommand = new RelayCommand(parameter => OpenNoteDialog(parameter as AnalysisEntryViewModel),
            parameter => !IsBusy && parameter is AnalysisEntryViewModel { IsAction: true });
        SaveNoteCommand = new AsyncCommand(SaveNoteAsync, () => !IsBusy && IsNoteDialogOpen);
        DiscardNoteCommand = new RelayCommand(_ => CloseNoteDialog(), _ => !IsBusy);
    }

    private void OpenNoteDialog(AnalysisEntryViewModel? entry)
    {
        if (entry?.Action is null)
            return;

        NoteEntry = entry;
        NoteText = entry.Note ?? string.Empty;
        ClearError();
        IsNoteDialogOpen = true;
    }

    private async Task SaveNoteAsync()
    {
        var entry = NoteEntry;
        if (entry?.Action is null)
            return;

        var previousNote = entry.Note ?? string.Empty;
        IsBusy = true;
        ClearError();
        try
        {
            entry.Action.UpdateNote(NoteText);
            await _actionService.UpdateAsync(entry.Action.Id, entry.Action);
            entry.SetNote(NoteText);
            CloseNoteDialog();
        }
        catch (Exception exception)
        {
            entry.Action.UpdateNote(previousNote);
            ErrorMessage = $"Could not save note: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }

    }

    private async Task SetActionRateAsync(object? parameter)
    {
        if (parameter is not ActionRateOptionViewModel option || option.Entry.Action is null)
            return;

        var previousRate = option.Entry.Rate;
        IsBusy = true;
        ClearError();
        try
        {
            option.Entry.Action.UpdateRate(option.Rate);
            await _actionService.UpdateAsync(option.Entry.Action.Id, option.Entry.Action);
            option.Entry.SetRate(option.Rate);
            RefreshFilteredActionEntries();
        }
        catch (Exception exception)
        {
            option.Entry.Action.UpdateRate(previousRate);
            ErrorMessage = $"Could not update action rate: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CloseNoteDialog()
    {
        IsNoteDialogOpen = false;
        NoteEntry = null;
        NoteText = string.Empty;
        ClearError();
    }

    public bool TryRecordHotkey(string hotkey)
    {
        if (IsBusy || !HasMatch || IsAnyDialogOpen || IsClipTab || string.IsNullOrWhiteSpace(hotkey))
            return false;

        var normalizedHotkey = NormalizeHotkey(hotkey);
        var sourceButtons = IsEventsTab ? EventButtons : ActionButtons;
        var matchingButtons = sourceButtons
            .Where(button => !string.IsNullOrWhiteSpace(button.HotKey))
            .Where(button => NormalizeHotkey(button.HotKey!) == normalizedHotkey)
            .ToArray();

        if (matchingButtons.Length == 0)
            return false;
        if (matchingButtons.Length > 1)
        {
            ErrorMessage = $"The hotkey '{hotkey}' is assigned to more than one button in this template.";
            return true;
        }

        RecordButtonCommand.Execute(matchingButtons[0]);
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event System.Action<double>? SeekRequested;
    public event System.Action? TogglePlaybackRequested;
    public event System.Action? BackRequested;

    public ObservableCollection<Button> EventButtons { get; } = [];
    public ObservableCollection<Button> ActionButtons { get; } = [];
    public ObservableCollection<AnalysisEntryViewModel> EventEntries { get; } = [];
    public ObservableCollection<AnalysisEntryViewModel> ActionEntries { get; } = [];
    public ObservableCollection<AnalysisEntryViewModel> FilteredActionEntries { get; } = [];
    public ObservableCollection<EventTimelineLaneViewModel> EventTimelineLanes { get; } = [];
    public ObservableCollection<Player> Players { get; } = [];
    public ObservableCollection<PlayerChoiceViewModel> PlayerChoices { get; } = [];
    public ObservableCollection<PlayerFilterOptionViewModel> PlayerFilterOptions { get; } = [];
    public ObservableCollection<ButtonFilterOptionViewModel> ButtonFilterOptions { get; } = [];
    public IReadOnlyList<ActionRate> ActionRates { get; } = Enum.GetValues<ActionRate>();

    public ICommand RecordButtonCommand { get; }
    public ICommand EditEntryCommand { get; }
    public ICommand SaveEntryCommand { get; }
    public ICommand DiscardEditCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand SeekToEntryCommand { get; }
    public ICommand TogglePlaybackCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand SelectPlayerCommand { get; }
    public ICommand SelectActionPlayerFilterCommand { get; }
    public ICommand SelectActionButtonFilterCommand { get; }
    public ICommand ToggleActionRateFilterCommand { get; }
    public ICommand ClearActionFiltersCommand { get; }
    public ICommand SelectEventsTabCommand { get; }
    public ICommand SelectActionsTabCommand { get; }
    public ICommand SelectClipsTabCommand { get; }
    public ICommand SelectActionClipCommand { get; }
    public ICommand SetActionRateCommand { get; }
    public ICommand OpenNoteCommand { get; }
    public ICommand SaveNoteCommand { get; }
    public ICommand DiscardNoteCommand { get; }

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
        set
        {
            if (SetProperty(ref _selectedPlayer, value))
            {
                OnPropertyChanged(nameof(SelectedPlayerLabel));
                foreach (var choice in PlayerChoices)
                    choice.IsSelected = choice.Player.Id == value?.Id;
            }
        }
    }

    public PlayerFilterOptionViewModel? SelectedActionPlayerFilter
    {
        get => _selectedActionPlayerFilter;
        set
        {
            if (SetProperty(ref _selectedActionPlayerFilter, value))
            {
                OnPropertyChanged(nameof(HasActiveActionFilters));
                RefreshFilteredActionEntries();
            }
        }
    }

    public ButtonFilterOptionViewModel? SelectedActionButtonFilter
    {
        get => _selectedActionButtonFilter;
        set
        {
            if (SetProperty(ref _selectedActionButtonFilter, value))
            {
                OnPropertyChanged(nameof(HasActiveActionFilters));
                RefreshFilteredActionEntries();
            }
        }
    }

    public ActionRate? ActionRateFilter
    {
        get => _actionRateFilter;
        private set
        {
            if (SetProperty(ref _actionRateFilter, value))
            {
                OnPropertyChanged(nameof(IsGoodRateFilterSelected));
                OnPropertyChanged(nameof(IsNaturalRateFilterSelected));
                OnPropertyChanged(nameof(IsBadRateFilterSelected));
                OnPropertyChanged(nameof(HasActiveActionFilters));
                RefreshFilteredActionEntries();
            }
        }
    }

    public bool IsGoodRateFilterSelected => ActionRateFilter == ActionRate.Good;
    public bool IsNaturalRateFilterSelected => ActionRateFilter == ActionRate.Natural;
    public bool IsBadRateFilterSelected => ActionRateFilter == ActionRate.Bad;
    public bool HasFilteredActions => FilteredActionEntries.Count > 0;
    public int FilteredActionCount => FilteredActionEntries.Count;
    public bool HasActiveActionFilters =>
        (SelectedActionPlayerFilter?.Player is not null) ||
        (SelectedActionButtonFilter?.Button is not null) ||
        ActionRateFilter is not null;
    public AnalysisEntryViewModel? SelectedActionClip
    {
        get => _selectedActionClip;
        private set
        {
            if (ReferenceEquals(_selectedActionClip, value))
                return;

            if (_selectedActionClip is not null)
                _selectedActionClip.IsSelectedClip = false;
            if (!SetProperty(ref _selectedActionClip, value))
                return;

            if (_selectedActionClip is not null)
                _selectedActionClip.IsSelectedClip = true;
            OnPropertyChanged(nameof(ClipStartSeconds));
            OnPropertyChanged(nameof(ClipEndSeconds));
            OnPropertyChanged(nameof(ClipDurationText));
            OnPropertyChanged(nameof(PlaybackSliderMinimum));
            OnPropertyChanged(nameof(PlaybackSliderMaximum));
            OnPropertyChanged(nameof(PlaybackRangeText));
            OnPropertyChanged(nameof(IsPlaybackSliderEnabled));
            PlaybackPositionSeconds = _selectedActionClip?.StartSeconds ?? PlaybackPositionSeconds;
        }
    }

    public bool IsClipTab => SelectedTabIndex == 2;
    public bool IsActionOrClipTab => IsActionTab || IsClipTab;
    public bool IsPlaybackSliderEnabled => HasMatch && (!IsClipTab || SelectedActionClip is not null);
    public double ClipStartSeconds => SelectedActionClip?.StartSeconds ?? 0;
    public double ClipEndSeconds => SelectedActionClip?.EndSeconds ?? PlaybackDurationSeconds;
    public string ClipDurationText => SelectedActionClip is null
        ? "Select an action to view its clip"
        : $"{FormatClock(SelectedActionClip.StartSeconds)} – {FormatClock(SelectedActionClip.EndSeconds)}";
    public string PlaybackRangeText => IsClipTab ? ClipDurationText : DurationText;
    public double PlaybackSliderMinimum => IsClipTab && SelectedActionClip is not null
        ? SelectedActionClip.StartSeconds
        : 0;
    public double PlaybackSliderMaximum => IsClipTab && SelectedActionClip is not null
        ? Math.Max(SelectedActionClip.StartSeconds, SelectedActionClip.EndSeconds)
        : PlaybackDurationSeconds;

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
                OnPropertyChanged(nameof(IsVideoVisible));
                NotifyCommands();
            }
        }
    }

    public bool IsNoteDialogOpen
    {
        get => _isNoteDialogOpen;
        private set
        {
            if (SetProperty(ref _isNoteDialogOpen, value))
            {
                OnPropertyChanged(nameof(IsAnyDialogOpen));
                OnPropertyChanged(nameof(IsVideoVisible));
                NotifyCommands();
            }
        }
    }

    public AnalysisEntryViewModel? NoteEntry
    {
        get => _noteEntry;
        private set => SetProperty(ref _noteEntry, value);
    }

    public string NoteText
    {
        get => _noteText;
        set => SetProperty(ref _noteText, value);
    }

    public bool IsAnyDialogOpen => IsEditDialogOpen || IsNoteDialogOpen;
    public bool IsVideoVisible => HasVideo && !IsAnyDialogOpen;
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
    public string SelectedPlayerLabel => SelectedPlayer is null
        ? "No player selected"
        : $"#{SelectedPlayer.Number} {SelectedPlayer.Name}";
    public bool IsActionTab => SelectedTabIndex == 1;
    public bool IsEventsTab => SelectedTabIndex == 0;
    public string PendingPressButtonName => _pendingDoublePressButton?.Name ?? string.Empty;
    public bool HasPendingDoublePress => HasPendingPress;
    public bool IsNotBusy => !IsBusy;
    public string CurrentTimeText => FormatClock(PlaybackPositionSeconds);
    public string DurationText => FormatClock(PlaybackDurationSeconds);
    public string PlaybackButtonText => IsPlaying ? "Pause" : "Play";
    public Geometry PlaybackIconGeometry => IsPlaying ? PauseGeometry : PlayGeometry;
    public string PendingPressText => _pendingDoublePressButton is null
        ? string.Empty
        : $"First press recorded for {_pendingDoublePressButton.Name}. Press it again to set the end.";

    public void SkipPlayback(double seconds) =>
        PlaybackPositionSeconds = Math.Max(0, PlaybackPositionSeconds + seconds);
    public double PlaybackPositionSeconds
    {
        get => _playbackPositionSeconds;
        set
        {
            var boundedPosition = Math.Clamp(value, PlaybackSliderMinimum,
                PlaybackSliderMaximum > 0 ? PlaybackSliderMaximum : double.MaxValue);
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
            {
                OnPropertyChanged(nameof(DurationText));
                OnPropertyChanged(nameof(PlaybackRangeText));
                OnPropertyChanged(nameof(PlaybackSliderMaximum));
                OnPropertyChanged(nameof(IsPlaybackSliderEnabled));
                OnPropertyChanged(nameof(ClipEndSeconds));
                RefreshTimelineLanes();
            }
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                ClearPendingPress();
                OnPropertyChanged(nameof(IsActionTab));
                OnPropertyChanged(nameof(IsEventsTab));
                OnPropertyChanged(nameof(IsClipTab));
                OnPropertyChanged(nameof(PlaybackRangeText));
                OnPropertyChanged(nameof(IsActionOrClipTab));
                OnPropertyChanged(nameof(PlaybackSliderMinimum));
                OnPropertyChanged(nameof(PlaybackSliderMaximum));
                PlaybackPositionSeconds = _playbackPositionSeconds;
                OnPropertyChanged(nameof(IsPlaybackSliderEnabled));
                NotifyCommands();
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                OnPropertyChanged(nameof(PlaybackButtonText));
                OnPropertyChanged(nameof(PlaybackIconGeometry));
            }
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
            SelectedActionClip = null;
            EventEntries.Clear();
            ActionEntries.Clear();
            FilteredActionEntries.Clear();
            EventButtons.Clear();
            ActionButtons.Clear();
            Players.Clear();
            PlayerChoices.Clear();
            PlayerFilterOptions.Clear();
            ButtonFilterOptions.Clear();
            ActionRateFilter = null;

            var buttons = await _buttonService.GetTemplateButtonsAsync(match.TemplateId);
            foreach (var button in buttons.Where(button => button.Type == ButtonType.Event))
                EventButtons.Add(button);
            foreach (var button in buttons.Where(button => button.Type == ButtonType.Action))
                ActionButtons.Add(button);
            ButtonFilterOptions.Add(new ButtonFilterOptionViewModel(null));
            foreach (var button in ActionButtons)
                ButtonFilterOptions.Add(new ButtonFilterOptionViewModel(button));
            SelectedActionButtonFilter = ButtonFilterOptions[0];
            OnPropertyChanged(nameof(HasEventButtons));
            OnPropertyChanged(nameof(HasActionButtons));

            var team = await _teamService.GetByIdWithPlayersAsync(match.TeamId)
                ?? throw new InvalidOperationException("The match team could not be found.");
            foreach (var player in team.Players.OrderBy(player => player.Number).ThenBy(player => player.Name))
            {
                Players.Add(player);
                PlayerChoices.Add(new PlayerChoiceViewModel(player));
            }
            PlayerFilterOptions.Add(new PlayerFilterOptionViewModel(null));
            foreach (var player in Players)
                PlayerFilterOptions.Add(new PlayerFilterOptionViewModel(player));
            SelectedActionPlayerFilter = PlayerFilterOptions[0];
            SelectedPlayer = Players.FirstOrDefault();

            var events = await _eventService.GetMatchEventsAsync(match.Id);
            foreach (var domainEvent in events.OrderBy(domainEvent => domainEvent.StartTime))
                EventEntries.Add(AnalysisEntryViewModel.FromEvent(domainEvent, FindButton(domainEvent.ButtonId)));

            var actions = await _actionService.GetMatchActionsAsync(match.Id);
            foreach (var action in actions.OrderBy(action => action.StartTime))
                ActionEntries.Add(AnalysisEntryViewModel.FromAction(
                    action, FindButton(action.ButtonId), Players.FirstOrDefault(player => player.Id == action.PlayerId)));

            RefreshFilteredActionEntries();
            OnPropertyChanged(nameof(HasEvents));
            OnPropertyChanged(nameof(HasActions));
            OnPropertyChanged(nameof(HasPlayers));
            RefreshTimelineLanes();
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
        var maximum = PlaybackSliderMaximum > 0 ? PlaybackSliderMaximum : double.MaxValue;
        _playbackPositionSeconds = Math.Clamp(positionSeconds, PlaybackSliderMinimum, maximum);
        OnPropertyChanged(nameof(PlaybackPositionSeconds));
        OnPropertyChanged(nameof(CurrentTimeText));
        IsPlaying = isPlaying;
    }

    public bool IsSelectedClipPlaybackComplete(double positionSeconds) =>
        IsClipTab && SelectedActionClip is not null &&
        positionSeconds >= SelectedActionClip.EndSeconds;

    public double? SelectedClipEndSeconds => IsClipTab ? SelectedActionClip?.EndSeconds : null;

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
                OnPropertyChanged(nameof(PendingPressButtonName));
                OnPropertyChanged(nameof(HasPendingDoublePress));
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
                RefreshTimelineLanes();
            }
            else
            {
                var action = await _actionService.CreateAsync(new Action(
                    match.Id, button.Id, SelectedPlayer!.Id, ActionRate.Natural, start, end, null));
                InsertByStartTime(ActionEntries, AnalysisEntryViewModel.FromAction(action, button, SelectedPlayer));
                RefreshFilteredActionEntries();
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
                RefreshTimelineLanes();
            }
            else if (entry.Action is not null)
            {
                ActionEntries.Remove(entry);
                var updatedEntry = AnalysisEntryViewModel.FromAction(entry.Action, entry.Button, EditPlayer);
                if (SelectedActionClip == entry)
                    SelectedActionClip = updatedEntry;
                InsertByStartTime(ActionEntries, updatedEntry);
                RefreshFilteredActionEntries();
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

    private async Task DeleteEntryAsync(object? parameter)
    {
        if (parameter is not AnalysisEntryViewModel entry)
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
                RefreshTimelineLanes();
            }
            else if (entry.Action is not null)
            {
                await _actionService.RemoveAsync(entry.Action.Id);
                ActionEntries.Remove(entry);
                if (SelectedActionClip == entry)
                    SelectedActionClip = null;
                RefreshFilteredActionEntries();
                OnPropertyChanged(nameof(HasActions));
            }

            if (EditingEntry == entry)
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

    private void RefreshFilteredActionEntries()
    {
        var playerId = SelectedActionPlayerFilter?.Player?.Id;
        var buttonId = SelectedActionButtonFilter?.Button?.Id;
        var matches = ActionEntries.Where(entry =>
                (playerId is null || entry.Player?.Id == playerId) &&
                (buttonId is null || entry.Button?.Id == buttonId) &&
                (ActionRateFilter is null || entry.Rate == ActionRateFilter))
            .ToArray();

        FilteredActionEntries.Clear();
        foreach (var entry in matches)
            FilteredActionEntries.Add(entry);

        if (SelectedActionClip is not null && !matches.Contains(SelectedActionClip))
            SelectedActionClip = null;

        OnPropertyChanged(nameof(HasFilteredActions));
        OnPropertyChanged(nameof(FilteredActionCount));
    }

    private void RefreshTimelineLanes()
    {
        var existingLanes = EventTimelineLanes.ToDictionary(lane => lane.Button.Id);
        var orderedLanes = EventButtons.Select(button =>
        {
            if (!existingLanes.TryGetValue(button.Id, out var lane))
                lane = new EventTimelineLaneViewModel(button);

            lane.Entries.Clear();
            foreach (var entry in EventEntries.Where(entry => entry.Button?.Id == button.Id))
            {
                entry.UpdateTimelinePosition(PlaybackDurationSeconds, TimelineTrackWidth);
                lane.Entries.Add(entry);
            }
            return lane;
        }).ToArray();

        EventTimelineLanes.Clear();
        foreach (var lane in orderedLanes)
            EventTimelineLanes.Add(lane);

        OnPropertyChanged(nameof(HasEvents));
    }

    private static string NormalizeHotkey(string hotkey)
    {
        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var modifiers = parts
            .Take(Math.Max(0, parts.Length - 1))
            .Select(part => part.Equals("Control", StringComparison.OrdinalIgnoreCase) ? "Ctrl" : part)
            .OrderBy(part => part, StringComparer.OrdinalIgnoreCase);
        var key = parts.LastOrDefault() ?? string.Empty;
        if (key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1]))
            key = key[1].ToString();
        return string.Join("+", modifiers.Append(key)).ToUpperInvariant();
    }

    private void ClearPendingPress()
    {
        _pendingDoublePressButton = null;
        _pendingDoublePressStart = null;
        OnPropertyChanged(nameof(PendingPressButtonName));
        OnPropertyChanged(nameof(HasPendingDoublePress));
        OnPropertyChanged(nameof(PendingPressText));
        OnPropertyChanged(nameof(HasPendingPress));
    }

    private void CloseEditDialog()
    {
        IsEditDialogOpen = false;
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
            DeleteEntryCommand,
            SeekToEntryCommand, TogglePlaybackCommand, BackCommand, SelectPlayerCommand,
            SelectEventsTabCommand, SelectActionsTabCommand, SetActionRateCommand,
            OpenNoteCommand, SaveNoteCommand, DiscardNoteCommand,
            SelectActionPlayerFilterCommand, SelectActionButtonFilterCommand,
            ToggleActionRateFilterCommand, ClearActionFiltersCommand,
            SelectClipsTabCommand, SelectActionClipCommand
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

    public sealed class AnalysisEntryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

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
            RateOptions = Enum.GetValues<ActionRate>()
                .Select(rate => new ActionRateOptionViewModel(this, rate))
                .ToArray();
        }

        public Event? Event { get; }
        public Action? Action { get; }
        public Button? Button { get; }
        public Player? Player { get; private set; }
        public double StartSeconds { get; private set; }
        public double EndSeconds { get; private set; }
        public string? Note { get; private set; }
        public ActionRate Rate { get; private set; } = ActionRate.Natural;
        public IReadOnlyList<ActionRateOptionViewModel> RateOptions { get; private set; } = [];
        public string ButtonName => Button?.Name ?? "Archived button";
        public string HotkeyLabel => Button?.HotKey ?? string.Empty;
        public string TimeRange => $"{FormatEditableTime(StartSeconds)} – {FormatEditableTime(EndSeconds)}";
        public string PlayerLabel => Player is null ? "Player not available" : $"#{Player.Number} {Player.Name}";
        public bool IsAction => Action is not null;
        private bool _isSelectedClip;
        public bool IsSelectedClip
        {
            get => _isSelectedClip;
            set
            {
                if (_isSelectedClip == value)
                    return;
                _isSelectedClip = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelectedClip)));
            }
        }
        public bool HasNote => !string.IsNullOrWhiteSpace(Note);
        public string? NoteDisplay => HasNote ? Note : "No note";
        public double TimelineLeft { get; private set; }
        public double TimelineWidth { get; private set; }

        public static AnalysisEntryViewModel FromEvent(Event domainEvent, Button? button) =>
            new(domainEvent, button);

        public static AnalysisEntryViewModel FromAction(Action action, Button? button, Player? player) =>
            new(action, button, player);

        public void UpdateTimelinePosition(double durationSeconds, double trackWidth)
        {
            var duration = durationSeconds > 0 ? durationSeconds : ManualTimelineDurationSeconds;
            TimelineLeft = Math.Clamp(StartSeconds / duration * trackWidth, 0, trackWidth - 10);
            TimelineWidth = Math.Clamp(
                Math.Max(10, (EndSeconds - StartSeconds) / duration * trackWidth),
                10,
                Math.Max(10, trackWidth - TimelineLeft));
        }

        public void SetRate(ActionRate rate)
        {
            Rate = rate;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rate)));
            foreach (var option in RateOptions)
                option.Refresh();
        }

        public void SetNote(string note)
        {
            Note = note;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Note)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasNote)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NoteDisplay)));
        }
    }

    public sealed class ActionRateOptionViewModel(
        AnalysisEntryViewModel entry,
        ActionRate rate) : INotifyPropertyChanged
    {
        private static readonly Geometry PerfectGeometry =
            Geometry.Parse("M12 2L15.1 8.3L22 9.3L17 14.2L18.2 21L12 17.7L5.8 21L7 14.2L2 9.3L8.9 8.3Z");
        private static readonly Geometry LikeGeometry =
            Geometry.Parse("M7 10V21H3V10H7ZM10 10L14 3C15.7 3 16.8 4.5 16.3 6.1L15.2 10H19C20.3 10 21.2 11.2 20.8 12.4L18.7 19.4C18.4 20.3 17.6 21 16.6 21H9C7.9 21 7 20.1 7 19V12C7 11.4 7.3 10.8 7.7 10.4L10 10Z");
        private static readonly Geometry DislikeGeometry =
            Geometry.Parse("M17 14V3H21V14H17ZM14 14L10 21C8.3 21 7.2 19.5 7.7 17.9L8.8 14H5C3.7 14 2.8 12.8 3.2 11.6L5.3 4.6C5.6 3.7 6.4 3 7.4 3H15C16.1 3 17 3.9 17 5V12C17 12.6 16.7 13.2 16.3 13.6L14 14Z");

        public event PropertyChangedEventHandler? PropertyChanged;
        public AnalysisEntryViewModel Entry { get; } = entry ?? throw new ArgumentNullException(nameof(entry));
        public ActionRate Rate { get; } = rate;
        public Geometry IconGeometry => Rate switch
        {
            ActionRate.Good => PerfectGeometry,
            ActionRate.Natural => LikeGeometry,
            ActionRate.Bad => DislikeGeometry,
            _ => Geometry.Parse("M0 0")
        };
        public string Label => Rate switch
        {
            ActionRate.Good => "Perfect",
            ActionRate.Natural => "Like",
            ActionRate.Bad => "Dislike",
            _ => Rate.ToString()
        };
        public string FilterLabel => Rate switch
        {
            ActionRate.Good => "Perfect",
            ActionRate.Natural => "Like",
            ActionRate.Bad => "Dislike",
            _ => Rate.ToString()
        };
        public IBrush IconBrush => Rate switch
        {
            ActionRate.Good => Brushes.LimeGreen,
            ActionRate.Natural => Brushes.Goldenrod,
            ActionRate.Bad => Brushes.IndianRed,
            _ => Brushes.Gray
        };
        public string ToolTip => Rate.ToString();
        public bool IsSelected => Entry.Rate == Rate;

        public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
    }

    public sealed class EventTimelineLaneViewModel(Button button)
    {
        public Button Button { get; } = button ?? throw new ArgumentNullException(nameof(button));
        public ObservableCollection<AnalysisEntryViewModel> Entries { get; } = [];
        public string Name => Button.Name ?? "Event";
        public string HotkeyLabel => Button.HotKey ?? string.Empty;
        public bool HasHotkey => Button.HasHotkey;
    }

    public sealed class PlayerChoiceViewModel(Player player) : INotifyPropertyChanged
    {
        private bool _isSelected;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Player Player { get; } = player ?? throw new ArgumentNullException(nameof(player));
        public string DisplayName => $"#{Player.Number} {Player.Name}";

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

    public sealed class PlayerFilterOptionViewModel(Player? player)
    {
        public Player? Player { get; } = player;
        public string DisplayName { get; } = player is null
            ? "All players"
            : $"#{player.Number} {player.Name}";
    }

    public sealed class ButtonFilterOptionViewModel(Button? button)
    {
        public Button? Button { get; } = button;
        public string DisplayName { get; } = button?.Name ?? "All actions";
    }
}
