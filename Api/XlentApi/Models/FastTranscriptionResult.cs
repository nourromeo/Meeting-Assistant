using System.Text.Json.Serialization;

namespace XlentApi.Models
{
    public class FastTranscriptionResult
    {
        [JsonPropertyName("fullText")]
        public string FullText { get; set; } = string.Empty;

        // A dictionary of unique speakers. Key = "1", Value = "Speaker 1" (default)
        // The frontend can easily update the Value to "Alice" or "Bob"
        [JsonPropertyName("speakers")]
        public Dictionary<string, string> Speakers { get; set; } = new();

        [JsonPropertyName("segments")]
        public List<FastTranscriptSegment> Segments { get; set; } = new();
    }

    public class FastTranscriptSegment
    {
        [JsonPropertyName("speakerId")]
        public string SpeakerId { get; set; } = string.Empty; // Maps to the key in the Speakers dictionary

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
