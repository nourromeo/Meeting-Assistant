using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using XlentApi.Interfaces;
using XlentApi.Models;

namespace XlentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranscriptionController : ControllerBase
    {
        //private readonly ITranscriptionService _transcriptionService;
        //private readonly ISummaryService _summaryService;
        //private readonly IPiiService _piiService;
        private readonly IFastTranscriptionService _fastTranscriptionService;

        public TranscriptionController(//ITranscriptionService transcriptionService, 
                                       //ISummaryService summaryService, 
                                       //IPiiService piiService,
            IFastTranscriptionService fastTranscriptionService)
        {
            //_transcriptionService = transcriptionService;
            //_summaryService = summaryService;
            //_piiService = piiService;
            _fastTranscriptionService = fastTranscriptionService;
        }

        [HttpPost("upload-fast")]
        public async Task<IActionResult> UploadAndTranscribe(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Ingen fil uppladdad.");
            }

            using var stream = file.OpenReadStream();

            try
            {
                var result = await _fastTranscriptionService.TranscribeAudioAsync(stream, file.FileName, "sv-SE");

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Ett fel uppstod vid transkribering: {ex.Message}");
            }
        }

        //[HttpPost("upload")]
        //public async Task<IActionResult> UploadFile(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("No file uploaded.");

        //    using (var stream = file.OpenReadStream())
        //    {
        //        var job = await _transcriptionService.StartBatchTranscriptionAsync(stream, file.FileName);
        //        return Ok(new { jobId = job.JobId, status = job.Status.ToString() });
        //    }
        //}

        //[HttpGet("{jobId}")]
        //public IActionResult GetJobStatus(string jobId)
        //{
        //    var job = _transcriptionService.GetJobStatus(jobId);
        //    if (job == null) return NotFound($"Job {jobId} not found");

        //    return Ok(job);
        //}

        //[HttpPost("summary")]
        //public async Task<IActionResult> GenerateSummary([FromBody] MeetingSummaryRequest request)
        //{
        //    if (request == null || request.Transcript == null || !request.Transcript.Any())
        //        return BadRequest("Invalid transcript data.");

        //    // Construct full text from segments
        //    var fullText = string.Join("\n", request.Transcript.Select(s => $"{s.SpeakerId}: {s.Text}"));

        //    // Mask PII before summarization? Or summarize raw?
        //    // Requirement: "Sammanfattning ska baseras på maskad text eller så ska det tydligt framgå om den baseras på raw text."
        //    // Let's summarize Raw for better quality, but mask the OUTPUT if needed.
        //    // Or safer: Mask FIRST, then summarize.
        //    // "Maskad text... innan det visas eller exporteras".
        //    // If we summarize raw, the summary might contain PII.
        //    // Safe approach: Mask PII in fullText, then Summarize.

        //    var maskedText = await _piiService.RedactPiiAsync(fullText);

        //    var summary = await _summaryService.GenerateSummaryAsync(maskedText, request.Language);

        //    return Ok(summary);
        //}

        //[HttpPost("redact")]
        //public async Task<IActionResult> RedactTranscript([FromBody] List<TranscriptSegment> transcript)
        //{
        //    if (transcript == null) return BadRequest();

        //    var redactedTranscript = new List<TranscriptSegment>();
        //    foreach (var seg in transcript)
        //    {
        //        var redactedText = await _piiService.RedactPiiAsync(seg.Text);
        //        redactedTranscript.Add(new TranscriptSegment
        //        {
        //            Text = redactedText,
        //            SpeakerId = seg.SpeakerId,
        //            StartOffsetSeconds = seg.StartOffsetSeconds,
        //            DurationSeconds = seg.DurationSeconds,
        //            IsFinal = seg.IsFinal
        //        });
        //    }
        //    return Ok(redactedTranscript);
        //}
    }
}



