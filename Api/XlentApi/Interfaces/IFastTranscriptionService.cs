using XlentApi.Models;

namespace XlentApi.Interfaces
{
    public interface IFastTranscriptionService
    {
        Task<FastTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string locale = "sv-SE");
    }
}
