using XlentApi.Models;

namespace XlentApi.Interfaces
{
    public interface ITranscriptionService
    {
        Task<TranscriptionJob> StartBatchTranscriptionAsync(Stream fileStream, string fileName);
        TranscriptionJob? GetJobStatus(string jobId);
        Task CreateRealTimeSessionAsync(string sessionId, Func<TranscriptSegment, Task> onSegmentRecognized, Func<string, Task> onError);
        Task PushAudioChunkAsync(string sessionId, byte[] audioData);
        Task StopRealTimeSessionAsync(string sessionId);
    }
}
