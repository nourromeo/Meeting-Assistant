using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;

namespace XlentWPF.ViewModels;

public partial class TranscriptViewModel : ObservableObject
{
    private readonly ITranscriptionService _transcriptionService;

    [ObservableProperty] private string _partialText = string.Empty;

    public TranscriptViewModel(ITranscriptionService transcriptionService)
    {
        _transcriptionService = transcriptionService;
        _transcriptionService.SegmentRecognised += OnSegmentRecognised;
        _transcriptionService.PartialResultReceived += text =>
            App.Current.Dispatcher.Invoke(() => PartialText = text);
    }

    private void OnSegmentRecognised(TranscriptSegment segment)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            PartialText = string.Empty; // clear the in-progress text
            AddSegment(segment);
        });
    }


    // -- Observable properties ------------------------------------------
    [ObservableProperty] private int _wordCount = 0;
    [ObservableProperty] private bool _isUploading = false;
    [ObservableProperty] private double _uploadProgress = 0;

    public ObservableCollection<TranscriptSegment> Segments { get; } = [];

    public async Task StartLiveTranscriptionAsync()
    {
        Segments.Clear();
        WordCount = 0;
        await _transcriptionService.StartLiveAsync();
    }

    // -- Public API (called by MainViewModel) --------------------------

    /// <summary>Adds a single segment — called for each live recognition result.</summary>
    public void AddSegment(TranscriptSegment segment)
    {
        Segments.Add(segment);
        // Use a more robust word counter if needed later,
        WordCount += segment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    


    public async Task StopLiveTranscriptionAsync()
    {
        // Tell the service to stop the engine
        await _transcriptionService.StopLiveAsync();

        // You can add logic here to "clean up" the UI if needed
        // e.g., adding a final message like "--- End of Session ---"
    }

    public async Task SimulateUploadAsync()
    {
        IsUploading = true;
        UploadProgress = 0;
        Segments.Clear();
        WordCount = 0;

        var progress = new Progress<int>(v => UploadProgress = v);
        var result = await _transcriptionService.TranscribeFileAsync("dummy_path", progress);

        foreach (var seg in result)
        {
            AddSegment(seg);
        }

        IsUploading = false;
        UploadProgress = 0;
    }

    // -- Commands ----------------------------------------------------------
    [RelayCommand]
    private void Clear()
    {
        Segments.Clear();
        WordCount = 0;
    }

    [RelayCommand]
    private void Download()
    {
        if (Segments.Count == 0) return;

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "transcript",
            DefaultExt = ".txt",
            Filter = "Text files|*.txt|All files|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        var lines = Segments.Select(s => $"[{s.Timestamp}] {s.SpeakerName}: {s.Text}");
        File.WriteAllLines(dlg.FileName, lines);
    }
}
