using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using XlentWPF.Models;
using XlentWPF.Dtos;

namespace XlentWPF.ViewModels;

public partial class SummaryViewModel : ObservableObject
{
    private const string ApiBaseUrl = "http://localhost:5239/";

    private readonly HttpClient _http;

    public SummaryViewModel()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl)
        };
    }

    [ObservableProperty] private SummaryResult? _summary = null;
    [ObservableProperty] private bool _isSummarizing = false;

    public async Task SummarizeAsync(IEnumerable<string> transcriptLines)
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
            var req = new MeetingSummaryRequestDto
            {
                Language = "en-US",
                Transcript = segments
            };

            using var resp = await _http.PostAsJsonAsync("api/summary", req);
            resp.EnsureSuccessStatusCode();

            var api = await resp.Content.ReadFromJsonAsync<MeetingSummaryResponseDto>();

            Summary = new SummaryResult
            {
                Overview = api?.Summary?.Trim() ?? "",
                KeyPoints = [], // API الحالي ما يرجّع KeyPoints
                ActionItems = api?.ActionItems ?? []
            };
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
        sb.AppendLine("=== ACTION ITEMS ===");
        foreach (var a in Summary.ActionItems) sb.AppendLine($"☐ {a}");

        File.WriteAllText(dlg.FileName, sb.ToString());
    }

}