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
    [ObservableProperty] private int _wordCount = 0;
    [ObservableProperty] private bool _isUploading = false;
    [ObservableProperty] private double _uploadProgress = 0;

    public ObservableCollection<TranscriptSegment> Segments { get; } = [];

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
            PartialText = string.Empty;
            AddSegment(segment);
        });
    }

    public void AddSegment(TranscriptSegment segment)
    {
        Segments.Add(segment);
        WordCount += segment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public async Task StartLiveTranscriptionAsync()
    {
        Segments.Clear();
        WordCount = 0;

        try
        {
            await _transcriptionService.StartLiveAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TranscriptViewModel] Could not start live transcription: {ex.Message}");
            App.Current.Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show(
                    $"Could not connect to the transcription service.\n\nMake sure the API is running on http://localhost:5001\n\nDetail: {ex.Message}",
                    "Connection Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning));
        }
    }

    public async Task StopLiveTranscriptionAsync()
    {
        await _transcriptionService.StopLiveAsync();
    }

    public async Task TranscribeFileAsync(string filePath)
    {
        IsUploading = true;
        UploadProgress = 0;
        Segments.Clear();
        WordCount = 0;

        try
        {
            var progress = new Progress<int>(v => UploadProgress = v);
            var result = await _transcriptionService.TranscribeFileAsync(filePath, progress);
            foreach (var seg in result) AddSegment(seg);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Transcription failed.\n\nDetail: {ex.Message}",
                "Upload Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        finally
        {
            IsUploading = false;
            UploadProgress = 0;
        }
    }

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

