using XlentWPF.Models;

namespace XlentWPF.Dtos
{
    public class RedactionRequestDto
    {
        public string Language { get; set; } = "sv";
        public PiiSettings Settings { get; set; } = new PiiSettings();
        public List<TranscriptSegmentDto> Transcripts { get; set; } = new List<TranscriptSegmentDto>();
    }
}
