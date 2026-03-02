using XlentWPF.Models;

namespace XlentWPF.Services.Interfaces
{
    public interface ISummaryService
    {
        /// <summary>
        /// Generate a structured summary from a list of transcript lines.
        /// Each line is expected to be in the format "SpeakerName: text".
        /// </summary>
        Task<SummaryResult> SummarizeAsync(
            IEnumerable<string> transcriptLines,
            CancellationToken cancellationToken = default);
    }
}
