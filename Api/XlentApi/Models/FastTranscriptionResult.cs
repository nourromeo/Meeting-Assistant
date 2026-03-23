using System.Text.Json.Serialization;

namespace XlentApi.Models
{
    public class FastTranscriptionResult
    {
        [JsonPropertyName("fullText")]
        public string FullText { get; set; } = string.Empty;


        [JsonPropertyName("segments")]
        public List<FastTranscriptSegment> Segments { get; set; } = new();
    }

    public class FastTranscriptSegment
    {
        //[JsonPropertyName("speakerId")]
        //public int? SpeakerId { get; set; }
        [JsonPropertyName("speakerId")]
        public string SpeakerId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("offsetMilliseconds")]
        public long OffsetMilliseconds { get; set; }

        [JsonPropertyName("durationMilliseconds")]
        public long DurationMilliseconds { get; set; }
    }

    internal class AzureFastTranscriptionResponse
    {
        [JsonPropertyName("combinedPhrases")]
        public List<AzureCombinedPhrase>? CombinedPhrases { get; set; }

        [JsonPropertyName("phrases")]
        public List<AzurePhrase>? Phrases { get; set; }
    }

    internal class AzureCombinedPhrase
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; } = string.Empty;
    }

    internal class AzurePhrase
    {
        [JsonPropertyName("speaker")]
        public int? Speaker { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("offsetMilliseconds")]
        public long OffsetMilliseconds { get; set; }

        [JsonPropertyName("durationMilliseconds")]
        public long DurationMilliseconds { get; set; }
    }
}
