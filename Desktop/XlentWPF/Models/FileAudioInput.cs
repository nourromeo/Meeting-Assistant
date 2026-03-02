namespace XlentWPF.Models
{
    public class FileAudioInput : AudioInput
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}
