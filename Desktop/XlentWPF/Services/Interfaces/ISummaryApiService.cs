using XlentWPF.Models;

namespace XlentWPF.Services.Interfaces
{
    public interface ISummaryApiService
    {
        Task<SummaryResult> SummarizeAsync(IEnumerable<TranscriptSegment> transcriptSegments);
    }
}
