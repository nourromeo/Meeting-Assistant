namespace XlentWPF.Models
{
    public class MicrophoneAudioInput : AudioInput
    {
        public byte[] MicAudioData { get; set; } = Array.Empty<byte>();
    }
}
