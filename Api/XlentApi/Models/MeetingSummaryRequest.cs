namespace XlentApi.Models
{
    public class MeetingSummaryRequest
    {
        public List<TranscriptSegment> Transcript { get; set; } = new List<TranscriptSegment>();
        public string Language { get; set; } = "en-US";
    }
}
