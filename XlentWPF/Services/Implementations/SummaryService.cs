
using System.Net.Http;
using System.Net.Http.Json;
using XlentWPF.Dtos;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.Services.Implementations
{
    public class SummaryService : ISummaryService
    {
        private readonly HttpClient _http;

        public SummaryService(HttpClient http) => _http = http;

        public async Task<SummaryResult> SummarizeAsync(IEnumerable<TranscriptSegmentDto> segments, string language = "sv-SE")
        {
            var response = await _http.PostAsJsonAsync("api/summary", new MeetingSummaryRequestDto
            {
                Transcript = segments.ToList(),
                Language = language
            });

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<MeetingSummaryResponseDto>();
            // map to SummaryResult

            return new SummaryResult
            {
                Overview = dto!.Summary,
                KeyPoints = dto.KeyPoints,
                ActionItems = dto.ActionItems
            };
        }
    }
}
