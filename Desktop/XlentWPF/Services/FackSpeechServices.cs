using XlentWPF.Models;

namespace XlentApi.Services
{
    public class FackSpeechServices : ISpeechService
    {
        public Task<string> TranscribeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<TranscriptionResult> TranscribeAsync(AudioInput input)
        {
            throw new NotImplementedException();
        }
    }
}
