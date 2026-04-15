namespace XlentWPF.Dtos
{
    public class TranscriptSegmentDto
    {
        public string Text { get; set; } = "";
        public string SpeakerId { get; set; } = "";
        public double StartOffsetSeconds { get; set; }
        public double DurationSeconds { get; set; }
        public bool IsFinal { get; set; }
    }
}