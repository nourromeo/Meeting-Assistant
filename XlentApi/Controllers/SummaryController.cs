using Microsoft.AspNetCore.Mvc;
using XlentApi.Interfaces;
using XlentApi.Models;
using System.Text;

namespace XlentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SummaryController : ControllerBase
    {
        private readonly ISummaryService _summaryService;

        public SummaryController(ISummaryService summaryService)
        {
            _summaryService = summaryService;
        }

        // POST: api/summary
        [HttpPost]
        public async Task<ActionResult<MeetingSummaryResponse>> Generate([FromBody] MeetingSummaryRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request.");

            if (request.Transcript == null || request.Transcript.Count == 0)
                return BadRequest("Transcript is required.");

            var fullText = BuildTranscriptText(request.Transcript);

            try
            {
                var result = await _summaryService.GenerateSummaryAsync(fullText, request.Language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Summary failed: {ex.Message}");
            }
        }

        private static string BuildTranscriptText(List<TranscriptSegment> transcript)
        {
            var sb = new StringBuilder();

            foreach (var seg in transcript)
            {
                sb.Append('[')
                  .Append(seg.SpeakerId)
                  .Append(" @ ")
                  .Append(seg.StartOffsetSeconds.ToString("0.0"))
                  .Append("s +")
                  .Append(seg.DurationSeconds.ToString("0.0"))
                  .Append("s] ")
                  .AppendLine(seg.Text);
            }

            return sb.ToString();
        }
    }
}