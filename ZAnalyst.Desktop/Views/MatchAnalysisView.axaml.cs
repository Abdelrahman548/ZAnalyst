using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using System.ComponentModel;
using ZAnalyst.Desktop.ViewModels;

namespace ZAnalyst.Desktop.Views;

public partial class MatchAnalysisView : UserControl
{
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    private DispatcherTimer? _playbackTimer;
    private MatchAnalysisViewModel? _viewModel;
    private string? _loadedVideoPath;

    public MatchAnalysisView()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
        Loaded += HandleLoaded;
        Unloaded += HandleUnloaded;
    }

    private void HandleDataContextChanged(object? sender, EventArgs eventArgs)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            _viewModel.SeekRequested -= SeekTo;
            _viewModel.TogglePlaybackRequested -= TogglePlayback;
        }
        _viewModel = DataContext as MatchAnalysisViewModel;
        if (_viewModel is null)
            return;

        _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        _viewModel.SeekRequested += SeekTo;
        _viewModel.TogglePlaybackRequested += TogglePlayback;
        if (IsLoaded)
            LoadVideo(_viewModel.VideoPath);
    }

    private void HandleLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (_viewModel is null)
            return;

        try
        {
            LoadVideo(_viewModel.VideoPath);
            _playbackTimer?.Start();
        }
        catch (Exception exception)
        {
            _viewModel.ReportVideoError(exception);
        }
    }

    private void HandleUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        _playbackTimer?.Stop();
        _mediaPlayer?.Pause();
        _viewModel?.UpdatePlaybackState(
            _mediaPlayer?.Time / 1000d ?? 0,
            GetDurationSeconds(),
            isPlaying: false);
        VideoView.MediaPlayer = null;
        _media?.Dispose();
        _media = null;
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
        _libVlc?.Dispose();
        _libVlc = null;
        _loadedVideoPath = null;
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(MatchAnalysisViewModel.VideoPath))
            LoadVideo(_viewModel?.VideoPath);
    }

    private void EnsurePlayer()
    {
        if (_mediaPlayer is not null)
            return;

        Core.Initialize();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        VideoView.MediaPlayer = _mediaPlayer;
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _playbackTimer.Tick += UpdatePlaybackState;
    }

    private void LoadVideo(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _mediaPlayer?.Stop();
            _media?.Dispose();
            _media = null;
            _loadedVideoPath = null;
            _viewModel?.UpdatePlaybackState(
                0, MatchAnalysisViewModel.ManualTimelineDurationSeconds, isPlaying: false);
            return;
        }
        if (path == _loadedVideoPath)
            return;

        try
        {
            EnsurePlayer();
            _mediaPlayer!.Stop();
            _media?.Dispose();
            _media = null;
            _loadedVideoPath = path;
            if (!File.Exists(path))
                throw new FileNotFoundException("The selected video file could not be found.", path);

            _media = new Media(_libVlc!, path, FromType.FromPath);
            _mediaPlayer.Media = _media;
            _viewModel?.UpdatePlaybackState(0, GetDurationSeconds(), isPlaying: false);
            _playbackTimer?.Start();
        }
        catch (Exception exception)
        {
            _loadedVideoPath = null;
            _viewModel?.UpdatePlaybackState(
                0, MatchAnalysisViewModel.ManualTimelineDurationSeconds, isPlaying: false);
            _viewModel?.ReportVideoError(exception);
        }
    }

    private void SeekTo(double positionSeconds)
    {
        if (_mediaPlayer is { IsSeekable: true } player)
            player.Time = (long)(positionSeconds * 1000);
    }

    private void TogglePlayback()
    {
        if (_mediaPlayer is null || string.IsNullOrWhiteSpace(_viewModel?.VideoPath))
            return;

        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Pause();
        else if (!_mediaPlayer.Play())
            _viewModel.ReportVideoError(new InvalidOperationException("The video player could not resume playback."));

        UpdatePlaybackState(this, EventArgs.Empty);
    }

    private void UpdatePlaybackState(object? sender, EventArgs eventArgs)
    {
        if (_mediaPlayer is null || _viewModel is null)
            return;

        _viewModel.UpdatePlaybackState(
            _mediaPlayer.Time / 1000d,
            GetDurationSeconds(),
            _mediaPlayer.IsPlaying);
    }

    private double GetDurationSeconds() =>
        _mediaPlayer is { Length: > 0 } player
            ? player.Length / 1000d
            : MatchAnalysisViewModel.ManualTimelineDurationSeconds;

    private void BackFiveSeconds_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (_viewModel is not null)
            _viewModel.PlaybackPositionSeconds -= 5;
    }

    private void ForwardFiveSeconds_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (_viewModel is not null)
            _viewModel.PlaybackPositionSeconds += 5;
    }

    private void EditTimelineEntry_DoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (sender is Button { DataContext: MatchAnalysisViewModel.AnalysisEntryViewModel entry })
            _viewModel?.EditEntryCommand.Execute(entry);
    }
}
