using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XlentWPF.Services.Interfaces;
using NAudio.Wave;
using System.Windows.Threading;


namespace XlentWPF.ViewModels;

public enum AppMode { Live, Upload }
public enum RecordingState { Idle, Recording, Paused, Stopped }

public partial class RecordingViewModel : ObservableObject
{
    private readonly IAudioService _audioService;
    private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _elapsedSeconds;

    public RecordingViewModel(IAudioService audioService)
    {
        _audioService = audioService;

        // Listen to the service for the waveform signal
        _audioService.AudioLevelChanged += OnAudioLevelChanged;

        // Initialize visualization bars
        for (int i = 0; i < 80; i++) WaveformBars.Add(10);

        _uiTimer.Tick += (_, _) =>
        {
            _elapsedSeconds++;
            TimerText = TimeSpan.FromSeconds(_elapsedSeconds).ToString(@"hh\:mm\:ss");
        };
    }

    private void OnAudioLevelChanged(float level)
    {
        // level is 0.0 to 1.0. We map it to our bar height (e.g., 3 to 40)
        App.Current.Dispatcher.Invoke(() =>
        {
            // Shift bars to the left and add new value at the end
            for (int i = 0; i < WaveformBars.Count - 1; i++)
                WaveformBars[i] = WaveformBars[i + 1];

            WaveformBars[WaveformBars.Count - 1] = 3 + (level * 35);
        });
    }

    [ObservableProperty] private string _timerText = "00:00:00";
    [ObservableProperty] private string _waveformLabel = "No signal";

    // Notify IsLiveMode / IsUploadMode whenever mode changes
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLiveMode))]
    [NotifyPropertyChangedFor(nameof(IsUploadMode))]
    private AppMode _currentMode = AppMode.Live;

    // Notify all derived properties and command CanExecute whenever state changes
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRecording))]
    [NotifyPropertyChangedFor(nameof(IsPaused))]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    [NotifyPropertyChangedFor(nameof(CanStop))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusDotColor))]
    [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private RecordingState _state = RecordingState.Idle;

    public ObservableCollectionEx<double> WaveformBars { get; } = [];

    // Logic-driven properties (State-derived)
    public bool IsRecording => State == RecordingState.Recording;
    public bool IsPaused    => State == RecordingState.Paused;
    public bool CanPause    => State == RecordingState.Recording || State == RecordingState.Paused;
    public bool CanStop     => State == RecordingState.Recording || State == RecordingState.Paused;

    // Mode helpers — bound to ToggleButton.IsChecked in HeaderBar
    public bool IsLiveMode   => CurrentMode == AppMode.Live;
    public bool IsUploadMode => CurrentMode == AppMode.Upload;

    // Status indicator (dot + label) in HeaderBar
    public string StatusText => State switch
    {
        RecordingState.Recording => "Recording",
        RecordingState.Paused    => "Paused",
        RecordingState.Stopped   => "Stopped",
        _                        => "Ready"
    };

    public string StatusDotColor => State switch
    {
        RecordingState.Recording => "#00E5A0",  // green
        RecordingState.Paused    => "#FFB800",  // amber
        _                        => "#6B7280"   // gray
    };

    public event Action? RecordingStarted;
    public event Action? RecordingStopped;

    // --- Mode switch commands (bound to the two ToggleButtons in HeaderBar) ---

    [RelayCommand]
    private void SwitchToLive() => CurrentMode = AppMode.Live;

    [RelayCommand]
    private void SwitchToUpload() => CurrentMode = AppMode.Upload;

    // --- Recording commands ---

    [RelayCommand]
    private async Task ToggleRecord()
    {
        if (IsRecording) await StopAsync();
        else await StartAsync();
    }

    private async Task StartAsync()
    {
        await _audioService.StartAsync();
        State = RecordingState.Recording;
        _uiTimer.Start();
        WaveformLabel = "Mic active";
        RecordingStarted?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        if (IsPaused)
        {
            _audioService.Resume();
            State = RecordingState.Recording;
            _uiTimer.Start();
            WaveformLabel = "Mic active";
        }
        else
        {
            _audioService.Pause();
            State = RecordingState.Paused;
            _uiTimer.Stop();
            WaveformLabel = "Paused";
            FlattenWaveform();
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        _audioService.Stop();
        State = RecordingState.Stopped;
        _uiTimer.Stop();
        _elapsedSeconds = 0;
        TimerText = "00:00:00";
        WaveformLabel = "No signal";
        FlattenWaveform();
        RecordingStopped?.Invoke();
    }

    private void FlattenWaveform()
    {
        for (int i = 0; i < WaveformBars.Count; i++) WaveformBars[i] = 3;
    }
}
