namespace XlentApi.Models
{
    public class TranscriptSegment
    {
        public string Text { get; set; } = string.Empty;
        public string SpeakerId { get; set; } = string.Empty;
        public string? DetectedLanguage { get; set; }
        public double StartOffsetSeconds { get; set; }
        public double DurationSeconds { get; set; }
        public bool IsFinal { get; set; }
    }
}
