using XlentApi.Models;

namespace XlentApi.Interfaces
{
    public interface IPiiService
    {
        Task<List<TranscriptSegment>> RedactTranscriptAsync(IEnumerable<TranscriptSegment> segments, string language, PiiSettings piiSettings = null, CancellationToken cancellationToken = default);
    }
}
