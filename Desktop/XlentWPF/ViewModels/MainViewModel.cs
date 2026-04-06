using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XlentWPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
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

        var lines = Transcript.Segments
            .Select(s => $"{s.SpeakerName}: {s.Text}")
            .ToList();

        var allText = string.Concat(Transcript.Segments.Select(s => s.Text));
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
}
