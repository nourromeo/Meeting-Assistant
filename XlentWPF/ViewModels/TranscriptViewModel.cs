using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.ViewModels;

public partial class TranscriptViewModel : ObservableObject
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly SemaphoreSlim _piiOperationLock = new(1, 1);
    private string _detectedLanguage = "sv";
    private bool _isTogglingPii;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(HasPartialText))]
    private string _partialText = string.Empty;

    [ObservableProperty] private int    _wordCount      = 0;
    [ObservableProperty] private bool   _isUploading    = false;
    [ObservableProperty] private double _uploadProgress = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PiiStatusLabel))]
    [NotifyPropertyChangedFor(nameof(PiiActionLabel))]
    private bool _isHidingPii = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PiiStatusLabel))]
    [NotifyPropertyChangedFor(nameof(PiiActionLabel))]
    [NotifyPropertyChangedFor(nameof(IsPiiToggleEnabled))]
    private bool _isPiiOperationInProgress;

    [ObservableProperty] private PiiSettings _piiSettings = new();

    public bool   ShowEmptyState  => !IsUploading && Segments.Count == 0;
    public bool   HasPartialText  => !string.IsNullOrEmpty(PartialText);
    public bool   IsPiiToggleEnabled => !IsPiiOperationInProgress;
    public string PiiStatusLabel => IsPiiOperationInProgress
        ? "Masking..."
        : IsHidingPii ? "Hidden" : "Visible";
    public string PiiActionLabel => IsHidingPii ? "Show" : "Hide";

    public ObservableCollection<TranscriptSegment> Segments { get; } = [];

    public TranscriptViewModel(ITranscriptionService transcriptionService)
    {
        _transcriptionService = transcriptionService;
        _transcriptionService.SegmentRecognised += OnSegmentRecognised;
        _transcriptionService.PartialResultReceived += text =>
            App.Current.Dispatcher.Invoke(() => PartialText = text);
        Segments.CollectionChanged += (_, e) =>
        {
            // Unsubscribe removed segments to prevent memory leak
            if (e.OldItems != null)
                foreach (TranscriptSegment s in e.OldItems)
                    s.PropertyChanged -= OnSegmentSpeakerNameChanged;

            OnPropertyChanged(nameof(ShowEmptyState));
        };
    }

    private void OnSegmentRecognised(TranscriptSegment segment)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            PartialText = string.Empty;
            AddSegment(segment);
        });

        _ = RedactSegmentIfNeededAsync(segment);
    }

    public void AddSegment(TranscriptSegment segment)
    {
        segment.PropertyChanged += OnSegmentSpeakerNameChanged;
        Segments.Add(segment);
        WordCount += segment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public async Task<IReadOnlyList<string>> GetSummaryLinesAsync()
    {
        if (Segments.Count == 0) return Array.Empty<string>();

        if (!IsHidingPii)
        {
            return Segments
                .Select(s => $"{s.SpeakerName}: {s.Text}")
                .ToList();
        }

        await _piiOperationLock.WaitAsync();
        try
        {
            var current = Segments.ToList();
            var redacted = await _transcriptionService.RedactAsync(
                current, _detectedLanguage, PiiSettings);

            var lines = new List<string>(current.Count);
            for (int i = 0; i < current.Count; i++)
            {
                var text = i < redacted.Count ? redacted[i].Text : current[i].Text;
                lines.Add($"{current[i].SpeakerName}: {text}");
            }

            return lines;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PII] Summary redaction failed, using current text: {ex.Message}");
            return Segments
                .Select(s => $"{s.SpeakerName}: {s.Text}")
                .ToList();
        }
        finally
        {
            _piiOperationLock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> RedactTextLinesAsync(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0) return Array.Empty<string>();

        await _piiOperationLock.WaitAsync();
        try
        {
            var textSegments = lines.Select(t => new TranscriptSegment
            {
                SpeakerName = "Summary",
                SpeakerIndex = 1,
                Timestamp = string.Empty,
                Text = t ?? string.Empty
            }).ToList();

            var redacted = await _transcriptionService.RedactAsync(
                textSegments, _detectedLanguage, PiiSettings);

            return redacted.Select(s => s.Text).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PII] Summary line redaction failed: {ex.Message}");
            return lines.ToList();
        }
        finally
        {
            _piiOperationLock.Release();
        }
    }

    private void OnSegmentSpeakerNameChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TranscriptSegment.SpeakerName)) return;
        if (sender is not TranscriptSegment changed) return;

        foreach (var s in Segments)
        {
            if (s.SpeakerIndex == changed.SpeakerIndex && s != changed)
                s.SpeakerName = changed.SpeakerName;
        }
    }

    public async Task StartLiveTranscriptionAsync(string? language = null)
    {
        _detectedLanguage = language?.StartsWith("sv", StringComparison.OrdinalIgnoreCase) == true
            ? "sv" : "en";

        Segments.Clear();
        WordCount = 0;

        try
        {
            await _transcriptionService.StartLiveAsync(language);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TranscriptViewModel] Could not start live transcription: {ex.Message}");
            App.Current.Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show(
                    $"Could not connect to the transcription service.\n\nMake sure the API is running on http://localhost:5239\n\nDetail: {ex.Message}",
                    "Connection Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning));
        }
    }

    public async Task StopLiveTranscriptionAsync()
    {
        await _transcriptionService.StopLiveAsync();
    }

    public async Task TranscribeFileAsync(string filePath, int expectedSpeakers = 2)
    {
        IsUploading = true;
        UploadProgress = 0;
        Segments.Clear();
        WordCount = 0;

        try
        {
            var progress = new Progress<int>(v => UploadProgress = v);
            var result = await _transcriptionService.TranscribeFileAsync(
                filePath, expectedSpeakers, progress);
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
        PartialText  = string.Empty;
        IsHidingPii  = false;
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

    [RelayCommand]
    private async Task ToggleHidePII()
    {
        if (Segments.Count == 0 || _isTogglingPii) return;
        _isTogglingPii = true;
        IsPiiOperationInProgress = true;

        await _piiOperationLock.WaitAsync();
        try
        {
            if (!IsHidingPii)
            {
                try
                {
                    var redacted = await _transcriptionService.RedactAsync(
                        Segments.ToList(), _detectedLanguage, PiiSettings);

                    for (int i = 0; i < Segments.Count && i < redacted.Count; i++)
                    {
                        Segments[i].OriginalText ??= Segments[i].Text;
                        Segments[i].Text = redacted[i].Text;
                    }
                    IsHidingPii = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PII] Redaction failed: {ex.Message}");
                }
            }
            else
            {
                foreach (var s in Segments)
                {
                    if (s.OriginalText is not null)
                    {
                        s.Text = s.OriginalText;
                        s.OriginalText = null;
                    }
                }
                IsHidingPii = false;
            }
        }
        finally
        {
            _piiOperationLock.Release();
            IsPiiOperationInProgress = false;
            _isTogglingPii = false;
        }
    }

    private async Task RedactSegmentIfNeededAsync(TranscriptSegment segment)
    {
        if (!IsHidingPii) return;

        await _piiOperationLock.WaitAsync();
        try
        {
            if (!IsHidingPii) return;

            segment.OriginalText ??= segment.Text;

            var redacted = await _transcriptionService.RedactAsync(
                new[] { segment }, _detectedLanguage, PiiSettings);

            if (IsHidingPii && redacted.Count > 0)
            {
                segment.Text = redacted[0].Text;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PII] Live segment redaction failed: {ex.Message}");
        }
        finally
        {
            _piiOperationLock.Release();
        }
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (Segments.Count == 0) return;
        var text = string.Join(Environment.NewLine,
            Segments.Select(s => $"[{s.Timestamp}] {s.SpeakerName}: {s.Text}"));
        System.Windows.Clipboard.SetText(text);
    }
}
