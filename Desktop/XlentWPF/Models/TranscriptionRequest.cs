namespace XlentWPF.Models
{
    public class TranscriptionRequest
    {
        public AudioInput AudioInput { get; set; } = null!;
        public string Language { get; set; } = "en-US";
    }
}
