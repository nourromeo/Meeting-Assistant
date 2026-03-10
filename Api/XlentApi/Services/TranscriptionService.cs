using Microsoft.CognitiveServices.Speech;
using XlentApi.Interfaces;
using XlentApi.Models;

namespace XlentApi.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly SpeechConfig speechConfig;

        public TranscriptionService(SpeechConfig speechConfig)
        {
            this.speechConfig = speechConfig;
        }
        public Task CreateRealTimeSessionAsync(string sessionId, Func<TranscriptSegment, Task> onSegmentRecognized, Func<string, Task> onError)
        {
            throw new NotImplementedException();
        }

        public TranscriptionJob? GetJobStatus(string jobId)
        {
            throw new NotImplementedException();
        }

        public Task PushAudioChunkAsync(string sessionId, byte[] audioData)
        {
            throw new NotImplementedException();
        }

        public Task<TranscriptionJob> StartBatchTranscriptionAsync(Stream fileStream, string fileName)
        {
            throw new NotImplementedException();
        }

        public Task StopRealTimeSessionAsync(string sessionId)
        {
            throw new NotImplementedException();
        }
    }
}
