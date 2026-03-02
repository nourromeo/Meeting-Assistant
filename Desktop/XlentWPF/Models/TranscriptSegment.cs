

namespace XlentWPF.Models
{
    public class TranscriptSegment
    {
        public string SpeakerName { get; set; } = string.Empty;
        public int SpeakerIndex { get; set; }   // 1, 2, 3 … for colour coding
        public string Timestamp { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
