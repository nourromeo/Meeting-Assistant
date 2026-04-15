using XlentWPF.Dtos;
using XlentWPF.Models;

namespace XlentWPF.Services.Interfaces
{
    public interface ISummaryService
    {
        Task<SummaryResult> SummarizeAsync(IEnumerable<TranscriptSegmentDto> segments, string language = "sv-SE");
    }
}
