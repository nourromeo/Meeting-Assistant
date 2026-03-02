using XlentApi.Models;

namespace XlentApi.Interfaces
{
    public interface ISummaryService
    {

        Task<MeetingSummaryResponse> GenerateSummaryAsync(string text, string language);
    }
}
