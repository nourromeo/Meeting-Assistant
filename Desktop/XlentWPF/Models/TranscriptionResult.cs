namespace XlentWPF.Models
{
    public class TranscriptionResult
    {
        public string Text { get; set; } = string.Empty;
        public double? Confidence { get; set; }
        public string Language { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }
}
