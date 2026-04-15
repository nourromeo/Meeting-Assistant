namespace XlentApi.Models
{
    public class TranscriptionJob
    {
        public string JobId { get; set; } = string.Empty;
        public TranscriptionStatus Status { get; set; }
        public List<TranscriptSegment> Segments { get; set; } = new List<TranscriptSegment>();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
