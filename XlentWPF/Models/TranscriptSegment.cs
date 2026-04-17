using CommunityToolkit.Mvvm.ComponentModel;

namespace XlentWPF.Models
{
    public partial class TranscriptSegment : ObservableObject
    {
        [ObservableProperty] private string _speakerName = string.Empty;
        [ObservableProperty] private string _text = string.Empty;
        [ObservableProperty] private string _timestamp = string.Empty;
        [ObservableProperty] private string? _detectedLanguage;

        // 1–8, used for colour coding
        public int SpeakerIndex { get; set; }

        // Stores the unredacted original text so PII hiding can be toggled off
        public string? OriginalText { get; set; }
    }
}
