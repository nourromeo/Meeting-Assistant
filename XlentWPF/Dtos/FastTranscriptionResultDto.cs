using System.Text.Json.Serialization;

namespace XlentWPF.Dtos
{
    public class FastTranscriptionResultDto
    {
        public string FullText { get; set; } = "";
        public List<FastTranscriptSegmentDto> Segments { get; set; } = [];
    }

    public class FastTranscriptSegmentDto
    {
        public int? SpeakerId { get; set; } = null;
        public string Text { get; set; } = "";

        [JsonPropertyName("offsetMilliseconds")]
        public double OffsetMilliseconds { get; set; }

        [JsonPropertyName("durationMilliseconds")]
        public double DurationMilliseconds { get; set; }

        [JsonPropertyName("detectedLanguage")]
        public string? DetectedLanguage { get; set; }
    }
}
