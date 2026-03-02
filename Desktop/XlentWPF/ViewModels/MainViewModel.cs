using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.ViewModels;


public partial class MainViewModel : ObservableObject
{
    // -- Child ViewModels -----------------------------------------------
    public RecordingViewModel Recording { get; } 
    public TranscriptViewModel Transcript { get; } 
    public SummaryViewModel Summary { get; } 

    // -- Constructor — wire events -------------------------------------
    public MainViewModel(
        RecordingViewModel recording,
        TranscriptViewModel transcript,
        SummaryViewModel summary)
    {
        
        Recording = recording;
        Transcript = transcript;
        Summary = summary;


        Recording.RecordingStarted += async () =>
        {
            try { await Transcript.StartLiveTranscriptionAsync(); }
            catch (Exception ex) { /* log or show error to user */ }
        };


        Recording.RecordingStopped += async () =>
         await Transcript.StopLiveTranscriptionAsync();

    }

    // -- Commands ----------------------------------------------------------

    /// <summary>Summarize button — passes transcript lines to SummaryViewModel.</summary>
    [RelayCommand]
    private async Task Summarize()
    {
        if (Transcript.Segments.Count == 0) return;

        var lines = Transcript.Segments
            .Select(s => $"{s.SpeakerName}: {s.Text}");

        await Summary.SummarizeAsync(lines);
    }

    /// <summary>Upload File button — delegates to TranscriptViewModel.</summary>
    [RelayCommand]
    private async Task UploadFile() =>
        await Transcript.SimulateUploadAsync();
}
