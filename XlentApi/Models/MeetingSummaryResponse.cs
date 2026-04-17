namespace XlentApi.Models
{
    public class MeetingSummaryResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> ActionItems { get; set; } = new List<string>();

    }
}
