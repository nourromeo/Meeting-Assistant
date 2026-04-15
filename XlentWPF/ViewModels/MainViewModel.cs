using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Threading;
using XlentWPF.Models;

namespace XlentWPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SemaphoreSlim _summarySyncLock = new(1, 1);
    private SummaryResult? _summaryBeforeMasking;

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

        _ = SyncSummaryWithPiiStateAsync();
    }

    private async Task SyncSummaryWithPiiStateAsync()
    {
        if (!await _summarySyncLock.WaitAsync(0)) return;

        try
        {
            if (Summary.Summary is null) return;

            if (Transcript.IsHidingPii)
            {
                if (_summaryBeforeMasking is null)
                {
                    _summaryBeforeMasking = CloneSummary(Summary.Summary);
                }

                var original = _summaryBeforeMasking;
                var linesToMask = new List<string>
                {
                    original.Overview
                };
                linesToMask.AddRange(original.KeyPoints);
                linesToMask.AddRange(original.ActionItems);

                var masked = await Transcript.RedactTextLinesAsync(linesToMask);
                if (masked.Count == 0) return;

                int idx = 0;
                var maskedSummary = new SummaryResult
                {
                    Overview = masked[idx++],
                    KeyPoints = original.KeyPoints
                        .Select(kp => idx < masked.Count ? masked[idx++] : kp)
                        .ToList(),
                    ActionItems = original.ActionItems
                        .Select(ai => idx < masked.Count ? masked[idx++] : ai)
                        .ToList()
                };

                Summary.Summary = maskedSummary;
            }
            else if (_summaryBeforeMasking is not null)
            {
                Summary.Summary = CloneSummary(_summaryBeforeMasking);
                _summaryBeforeMasking = null;
            }
        }
        finally
        {
            _summarySyncLock.Release();
        }
    }

    private static SummaryResult CloneSummary(SummaryResult source)
    {
        return new SummaryResult
        {
            Overview = source.Overview,
            KeyPoints = source.KeyPoints.ToList(),
            ActionItems = source.ActionItems.ToList()
        };
    }
}
