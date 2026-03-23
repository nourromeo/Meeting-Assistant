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

        // Start/stop transcription based on recording state
        Recording.RecordingStarted += async () =>
        {
            await Transcript.StartLiveTranscriptionAsync();
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

        // Auto-detect language: Swedish if any segment contains å, ä, or ö
        var allText = string.Concat(Transcript.Segments.Select(s => s.Text));
        var language = allText.Any(c => c is 'å' or 'ä' or 'ö' or 'Å' or 'Ä' or 'Ö')
            ? "sv-SE"
            : "en-US";

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
}