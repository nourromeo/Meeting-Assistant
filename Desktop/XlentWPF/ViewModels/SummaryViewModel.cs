using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;

namespace XlentWPF.ViewModels;

public partial class SummaryViewModel : ObservableObject
{

    private readonly ISummaryService _summaryService;

    public SummaryViewModel(ISummaryService summaryService)
    {
        _summaryService = summaryService;
    }


    // -- Observable properties --------------------------------------------
    [ObservableProperty] private SummaryResult? _summary = null;
    [ObservableProperty] private bool _isSummarizing = false;



    // -- Public API (called by MainViewModel) -----------------------------
    public async Task SummarizeAsync(IEnumerable<string> transcriptLines)
    {
        if (transcriptLines == null || !transcriptLines.Any()) return;

 
        IsSummarizing = true;
        Summary = null;

        try
        {
            Summary = await _summaryService.SummarizeAsync(transcriptLines);
        }
        catch (Exception ex)
        {
            // TODO: Handle errors (e.g., API offline)
            System.Diagnostics.Debug.WriteLine($"Summary failed: {ex.Message}");
        }
        finally
        {
            IsSummarizing = false;
        }
    }

    // -- Commands ------------------------------------------------
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
        sb.AppendLine("=== KEY POINTS ===");
        foreach (var p in Summary.KeyPoints) sb.AppendLine($"• {p}");
        sb.AppendLine();
        sb.AppendLine("=== ACTION ITEMS ===");
        foreach (var a in Summary.ActionItems) sb.AppendLine($"☐ {a}");

        File.WriteAllText(dlg.FileName, sb.ToString());
    }
}
