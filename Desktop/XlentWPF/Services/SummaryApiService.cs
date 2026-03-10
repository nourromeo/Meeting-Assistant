using System.Net.Http;
using System.Net.Http.Json;
using XlentWPF.Dtos;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.Services
{
    public class SummaryApiService : ISummaryApiService
    {
        private readonly HttpClient _http;

        public SummaryApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<SummaryResult> SummarizeAsync(IEnumerable<TranscriptSegment> transcriptSegments)
        {
            var segments = (transcriptSegments ?? Enumerable.Empty<TranscriptSegment>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .Select(s => new TranscriptSegmentDto
                {
                    SpeakerId = string.IsNullOrWhiteSpace(s.SpeakerName) ? "unknown" : s.SpeakerName.Trim(),
                    Text = s.Text.Trim(),
                    StartOffsetSeconds = 0.0,
                    DurationSeconds = 0.0,
                    IsFinal = true
                })
                .ToList();

            if (segments.Count == 0)
            {
                return new SummaryResult();
            }

            var req = new MeetingSummaryRequestDto
            {
                Language = "en-US",
                Transcript = segments
            };

            using var resp = await _http.PostAsJsonAsync("api/summary", req);
            resp.EnsureSuccessStatusCode();

            var api = await resp.Content.ReadFromJsonAsync<MeetingSummaryResponseDto>();

            return new SummaryResult
            {
                Overview = api?.Summary?.Trim() ?? "",
                KeyPoints = [],
                ActionItems = api?.ActionItems ?? []
            };
        }
    }
}