using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using System.Collections.Concurrent;
using XlentApi.Interfaces;
using XlentApi.Models;


namespace XlentApi.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly IFastTranscriptionService _fastTranscription;
        private readonly SpeechConfig _speechConfig;
        private readonly ILogger<TranscriptionService> _logger;

        private sealed record LiveSession(
        PushAudioInputStream PushStream,
        ConversationTranscriber Transcriber,
        CancellationTokenSource Cts);

        // In-memory job store 
        private readonly ConcurrentDictionary<string, TranscriptionJob> _jobs = new();

        // Active real-time sessions keyed by sessionId
        private readonly ConcurrentDictionary<string, LiveSession> _sessions = new();


        public TranscriptionService(
       IFastTranscriptionService fastTranscription,
       SpeechConfig speechConfig,
       ILogger<TranscriptionService> logger)
        {
            _fastTranscription = fastTranscription;
            _speechConfig = speechConfig;
            _logger = logger;
        }

        public async Task<TranscriptionJob> StartBatchTranscriptionAsync(Stream fileStream, string fileName)
        {
            var job = new TranscriptionJob
            {
                JobId = Guid.NewGuid().ToString("N"),
                Status = TranscriptionStatus.Queued,
                CreatedAt = DateTime.UtcNow
            };

            _jobs[job.JobId] = job;

            var buffer = new MemoryStream();
            await fileStream.CopyToAsync(buffer);
            buffer.Position = 0;

            _ = Task.Run(async () =>
            {
                job.Status = TranscriptionStatus.Processing;
                try
                {
                    var result = await _fastTranscription.TranscribeAudioAsync(
                        buffer, fileName, ["sv-SE", "en-US"]);

                    job.Segments = result.Segments
                        .Select(s => new TranscriptSegment
                        {
                            SpeakerId = s.SpeakerId?.ToString() ?? "unknown",
                            Text = s.Text,
                            StartOffsetSeconds = s.OffsetMilliseconds / 1000.0,
                            DurationSeconds = s.DurationMilliseconds / 1000.0,
                            IsFinal = true
                        }).ToList();

                    job.Status = TranscriptionStatus.Completed;
                }
                catch (Exception ex)
                {
                    job.Status = TranscriptionStatus.Failed;
                    job.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Batch transcription failed for job {JobId}", job.JobId);
                }
                finally
                {
                    await buffer.DisposeAsync();
                }
            });

            return job;
        }

     

        public TranscriptionJob? GetJobStatus(string jobId)
        {
            _jobs.TryGetValue(jobId, out var job);
            return job;
        }


        // ------------------------------------------------------------------ //
        //  REAL-TIME SESSION (Speech SDK push-stream)                         //
        // ------------------------------------------------------------------ //

        public async Task CreateRealTimeSessionAsync(
            string sessionId,
            Func<TranscriptSegment, Task> onSegmentRecognized,
            Func<string, Task> onError)
        {
            if (_sessions.ContainsKey(sessionId))
                throw new InvalidOperationException($"Session '{sessionId}' already exists.");

            // 16 kHz, 16-bit, mono — must match what the WPF AudioService captures
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16_000, 16, 1);
            var pushStream = AudioInputStream.CreatePushStream(audioFormat);
            var audioConfig = AudioConfig.FromStreamInput(pushStream);

            // ConversationTranscriber supports per-utterance speaker diarization
            var transcriber = new ConversationTranscriber(_speechConfig, audioConfig);

            transcriber.Transcribed += async (_, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
                {
                    var segment = new TranscriptSegment
                    {
                        Text = e.Result.Text,
                        SpeakerId = e.Result.SpeakerId,
                        StartOffsetSeconds = e.Result.OffsetInTicks / 10_000_000.0,
                        DurationSeconds = e.Result.Duration.TotalSeconds,
                        IsFinal = true
                    };
                    await onSegmentRecognized(segment);
                }
            };

            transcriber.Canceled += async (_, e) =>
            {
                if (e.Reason == CancellationReason.Error)
                    await onError($"Azure Speech error {e.ErrorCode}: {e.ErrorDetails}");
            };

            var cts = new CancellationTokenSource();
            _sessions[sessionId] = new LiveSession(pushStream, transcriber, cts);

            await transcriber.StartTranscribingAsync();
        }


        public Task PushAudioChunkAsync(string sessionId, byte[] audioData)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
                session.PushStream.Write(audioData);

            return Task.CompletedTask;
        }

        public async Task StopRealTimeSessionAsync(string sessionId)
        {
            if (!_sessions.TryRemove(sessionId, out var session)) return;

            session.PushStream.Close();
            await session.Transcriber.StopTranscribingAsync();
            session.Transcriber.Dispose();
            session.Cts.Dispose();
        }
    }
}
