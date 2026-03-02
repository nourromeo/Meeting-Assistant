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

        // تشغيل/إيقاف التفريغ حسب التسجيل
        Recording.RecordingStarted += async () =>
        {
            try { await Transcript.StartLiveTranscriptionAsync(); }
            catch { /* تجاهل/سجل */ }
        };

        Recording.RecordingStopped += async () =>
        {
            try { await Transcript.StopLiveTranscriptionAsync(); }
            catch { /* تجاهل/سجل */ }
        };
    }

    [RelayCommand]
    private async Task Summarize()
    {
        if (Transcript.Segments.Count == 0) return;

        var lines = Transcript.Segments
            .Select(s => $"{s.SpeakerName}: {s.Text}");

        await Summary.SummarizeAsync(lines);
    }

    [RelayCommand]
    private async Task UploadFile()
        => await Transcript.SimulateUploadAsync();
}