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
        private readonly IFastTranscriptionService _fastTranscriptionService;
        private readonly ITranscriptionService _transcriptionService;
       

        public TranscriptionController(
            IFastTranscriptionService fastTranscriptionService,
            ITranscriptionService transcriptionService
            )
        {
            _fastTranscriptionService = fastTranscriptionService;
            _transcriptionService = transcriptionService;
            
        }

        [HttpPost("upload-fast")]
        public async Task<IActionResult> UploadAndTranscribe(
            IFormFile file ,
            [FromQuery] string[]? languages,
            [FromQuery] string[]? customPhrases,
            [FromQuery] int? expectedSpeakers)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Ingen fil uppladdad.");
            }

            var selectedLocales = languages?.Length > 0
                ? languages
                : new[] { "sv-SE", "en-US" };


            using var stream = file.OpenReadStream();

            try
            {
                var result = await _fastTranscriptionService.TranscribeAudioAsync(
                    stream, file.FileName, selectedLocales, customPhrases, expectedSpeakers);

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, $"Kommunikationsfel med Azure: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ett fel uppstod vid transkribering: {ex.Message}");
            }
        }

        // POST api/transcription/redact
        [HttpPost("redact")]
        public async Task<IActionResult> RedactTranscript([FromBody] List<TranscriptSegment> transcript)
        {
            if (transcript == null || transcript.Count == 0)
                return BadRequest("Tomt transkript.");

            var redacted = new List<TranscriptSegment>();
            foreach (var seg in transcript)
            {
                // För demo, returnera originaltexten. I verkligheten skulle du anropa en PII-redigeringsservice här.

                redacted.Add(new TranscriptSegment
                {
                    Text = seg.Text,
                    SpeakerId = seg.SpeakerId,
                    StartOffsetSeconds = seg.StartOffsetSeconds,
                    DurationSeconds = seg.DurationSeconds,
                    IsFinal = seg.IsFinal
                });
            }

            return Ok(redacted);
        }

        // POST api/transcription/upload  — async batch; poll GET /{jobId} for result
        [HttpPost("upload")]
        public async Task<IActionResult> UploadBatch(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Ingen fil uppladdad.");

            using var stream = file.OpenReadStream();
            var job = await _transcriptionService.StartBatchTranscriptionAsync(stream, file.FileName);
            return Accepted(new { jobId = job.JobId, status = job.Status.ToString() });
        }

        // GET api/transcription/{jobId}
        [HttpGet("{jobId}")]
        public IActionResult GetJobStatus(string jobId)
        {
            var job = _transcriptionService.GetJobStatus(jobId);
            if (job is null) return NotFound($"Jobb {jobId} hittades inte.");
            return Ok(job);
        }

    }
}



