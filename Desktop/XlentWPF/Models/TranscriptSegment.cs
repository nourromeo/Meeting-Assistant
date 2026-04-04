using CommunityToolkit.Mvvm.ComponentModel;

namespace XlentWPF.Models
{
    public partial class TranscriptSegment : ObservableObject
    {
        [ObservableProperty] private string _speakerName = string.Empty;
        public int SpeakerIndex { get; set; }   // 1, 2, 3 … for colour coding
        public string Timestamp { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
