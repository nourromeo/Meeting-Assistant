using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using XlentWPF.Dtos;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.ViewModels;

public partial class SummaryViewModel : ObservableObject
{
    private readonly ISummaryService _summaryService;
    public SummaryViewModel(ISummaryService summaryService)
    {
        _summaryService = summaryService;
    }

    [ObservableProperty] private SummaryResult? _summary = null;
    [ObservableProperty] private bool _isSummarizing = false;

    public async Task SummarizeAsync(IEnumerable<string> transcriptLines, string language = "sv-SE")
    {
        var segments = (transcriptLines ?? Array.Empty<string>())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(ToSegment)
            .ToList();

        if (segments.Count == 0) return;

        IsSummarizing = true;
        Summary = null;

        try
        {
            Summary = await _summaryService.SummarizeAsync(segments, language);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Summary failed: {ex.Message}");
        }
        finally
        {
            IsSummarizing = false;
        }
    }

    private static TranscriptSegmentDto ToSegment(string line)
    {
        var s = line.Trim();
        var speaker = "unknown";
        var text = s;

        var idx = s.IndexOf(':');
        if (idx > 0)
        {
            speaker = s[..idx].Trim();
            text = s[(idx + 1)..].Trim();
        }

        return new TranscriptSegmentDto
        {
            SpeakerId = speaker,
            Text = text,
            StartOffsetSeconds = 0.0,
            DurationSeconds = 0.0,
            IsFinal = true
        };
    }

    [RelayCommand]
    private void Clear() => Summary = null;

    [RelayCommand]
    private void Download()
    {
        if (Summary is null) return;

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "summary",
            DefaultExt = ".txt",
            Filter = "Text files|*.txt|All files|*.*"
        };

        if (dlg.ShowDialog() != true) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== OVERVIEW ===");
        sb.AppendLine(Summary.Overview);
        sb.AppendLine();

        if (Summary.KeyPoints?.Count > 0)
        {
            sb.AppendLine("=== KEY POINTS ===");
            foreach (var kp in Summary.KeyPoints) sb.AppendLine($"• {kp}");
            sb.AppendLine();
        }

        sb.AppendLine("=== ACTION ITEMS ===");
        foreach (var a in Summary.ActionItems) sb.AppendLine($"☐ {a}");

        File.WriteAllText(dlg.FileName, sb.ToString());
    }

}