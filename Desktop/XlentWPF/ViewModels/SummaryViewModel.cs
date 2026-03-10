using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.ViewModels;

public partial class SummaryViewModel : ObservableObject
{
    private readonly ISummaryApiService _summaryApiService;

    public SummaryViewModel(ISummaryApiService summaryApiService)
    {
        _summaryApiService = summaryApiService;
    }

    [ObservableProperty] private SummaryResult? _summary = null;
    [ObservableProperty] private bool _isSummarizing = false;

    public async Task SummarizeAsync(IEnumerable<TranscriptSegment> transcriptSegments)
    {
        if (transcriptSegments == null || !transcriptSegments.Any())
            return;

        IsSummarizing = true;
        Summary = null;

        try
        {
            Summary = await _summaryApiService.SummarizeAsync(transcriptSegments);
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