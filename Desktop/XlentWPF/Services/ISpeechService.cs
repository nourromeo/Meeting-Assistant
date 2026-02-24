using XlentApi.Models;

namespace XlentApi.Services
{
    public interface ISpeechService
    {
        Task<string> TranscribeAsync();

        Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request);

        Task<TranscriptionResult> TranscribeAsync(AudioInput input);
    }
}
