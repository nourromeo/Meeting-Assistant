using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Threading;

namespace XlentWPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SemaphoreSlim _summarySyncLock = new(1, 1);

    public RecordingViewModel Recording { get; }
    public TranscriptViewModel Transcript { get; }
    public SummaryViewModel Summary { get; }

    public MainViewModel(
        RecordingViewModel recording,
        TranscriptViewModel transcript,
        SummaryViewModel summary)
    {
        Recording = recording;
        Transcript = transcript;
        Summary = summary;
        Transcript.PropertyChanged += OnTranscriptPropertyChanged;

        // Func<Task> events — properly awaited, exceptions surface correctly
        // Language is null here: the API uses AutoDetectSourceLanguageConfig and
        // picks Swedish vs English per utterance automatically — no client-side guessing.
        Recording.RecordingStarted += async () =>
        {
            await Transcript.StartLiveTranscriptionAsync(language: null);
        };

        Recording.RecordingStopped += async () =>
        {
            await Transcript.StopLiveTranscriptionAsync();
        };
    }

    [RelayCommand]
    private async Task Summarize()
    {
        if (Transcript.Segments.Count == 0)
            return;

        var lines = await Transcript.GetSummaryLinesAsync();
        if (lines.Count == 0) return;

        var allText = string.Concat(lines);
        var language = DetectLanguage(allText);

        await Summary.SummarizeAsync(lines, language);
    }

    [RelayCommand]
    private async Task UploadFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select audio file",
            Filter = "Audio files|*.wav;*.mp3;*.ogg;*.m4a|All files|*.*"
        };

        if (dlg.ShowDialog() != true) return;

        await Transcript.TranscribeFileAsync(dlg.FileName);
    }

    // Auto-detect language by looking for Swedish-specific characters
    private static string DetectLanguage(string text) =>
        text.Any(c => c is 'å' or 'ä' or 'ö' or 'Å' or 'Ä' or 'Ö') ? "sv-SE" : "en-US";

    private void OnTranscriptPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TranscriptViewModel.IsHidingPii)) return;
        if (Summary.Summary is null) return;
        if (Transcript.Segments.Count == 0) return;

        _ = SyncSummaryWithPiiStateAsync();
    }

    private async Task SyncSummaryWithPiiStateAsync()
    {
        if (!await _summarySyncLock.WaitAsync(0)) return;

        try
        {
            var lines = await Transcript.GetSummaryLinesAsync();
            if (lines.Count == 0) return;

            var allText = string.Concat(lines);
            var language = DetectLanguage(allText);

            await Summary.SummarizeAsync(lines, language);
        }
        finally
        {
            _summarySyncLock.Release();
        }
    }
}
