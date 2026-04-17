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
            AudioConfig AudioConfig,
            ConversationTranscriber Transcriber,
            CancellationTokenSource Cts);

        
        private readonly ConcurrentDictionary<string, TranscriptionJob> _jobs = new();
        private static readonly TimeSpan JobTtl = TimeSpan.FromMinutes(30);

      
        private readonly ConcurrentDictionary<string, LiveSession> _sessions = new();

    
        private static readonly string[] RecognitionLanguages = ["sv-SE", "en-US"];


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
            PurgeExpiredJobs();

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
                        buffer, fileName, RecognitionLanguages);

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
            string? language,
            Func<TranscriptSegment, Task> onSegmentRecognized,
            Func<string, Task> onPartialResult,
            Func<string, Task> onError)
        {
            if (_sessions.ContainsKey(sessionId))
                throw new InvalidOperationException($"Session '{sessionId}' already exists.");

            // Clone config from the singleton so per-session changes don't bleed across sessions.
            // Re-apply all properties explicitly — FromSubscription only copies key/region.
            var config = BuildSessionConfig();

            // 16 kHz, 16-bit, mono — must match what the WPF AudioService captures
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16_000, 16, 1);
            var pushStream = AudioInputStream.CreatePushStream(audioFormat);
            var audioConfig = AudioConfig.FromStreamInput(pushStream);

            ConversationTranscriber transcriber;

            if (!string.IsNullOrWhiteSpace(language))
            {
                // Give AI maximum context/accuracy head-start when an explicit language is selected
                config.SpeechRecognitionLanguage = language;
                transcriber = new ConversationTranscriber(config, audioConfig);
            }
            else
            {
                // Use Azure's built-in Language Identification to auto-detect per utterance.
                var autoDetect = AutoDetectSourceLanguageConfig.FromLanguages(RecognitionLanguages);
                transcriber = new ConversationTranscriber(config, autoDetect, audioConfig);
            }

            // Intermediate/partial results — fires every few hundred ms while speaking
            transcriber.Transcribing += async (_, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizingSpeech &&
                    !string.IsNullOrWhiteSpace(e.Result.Text))
                {
                    await onPartialResult(e.Result.Text);
                }
            };

            // Final results — fires when the utterance segmentation silence is hit
            transcriber.Transcribed += async (_, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech &&
                    !string.IsNullOrWhiteSpace(e.Result.Text))
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
                {
                    _logger.LogError("Speech SDK canceled [{SessionId}] {Code}: {Details}",
                        sessionId, e.ErrorCode, e.ErrorDetails);
                    await onError($"Azure Speech error {e.ErrorCode}: {e.ErrorDetails}");
                }
            };

            var cts = new CancellationTokenSource();
            _sessions[sessionId] = new LiveSession(pushStream, audioConfig, transcriber, cts);

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

            await session.Transcriber.StopTranscribingAsync();
            session.Transcriber.Dispose();
            session.AudioConfig.Dispose();
            session.PushStream.Dispose();
            session.Cts.Dispose();
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                            //
        // ------------------------------------------------------------------ //

        private SpeechConfig BuildSessionConfig()
        {
            //var c = SpeechConfig.FromSubscription(
            //    _speechConfig.GetProperty(PropertyId.SpeechServiceConnection_Key),
            //    _speechConfig.GetProperty(PropertyId.SpeechServiceConnection_Region));

            var region = _speechConfig.GetProperty(PropertyId.SpeechServiceConnection_Region);
            var key = _speechConfig.GetProperty(PropertyId.SpeechServiceConnection_Key);

            // Use the v2 universal endpoint required for Continuous LID 
            var v2Endpoint = new Uri($"wss://{region}.stt.speech.microsoft.com/speech/universal/v2");
            var c = SpeechConfig.FromEndpoint(v2Endpoint, key);

            // Ensure live transcription can keep identifying language during the session
            // when AutoDetectSourceLanguageConfig is used (sv-SE/en-US switching).
            c.SetProperty(PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous");

            // Return raw text — profanity filter masks words and looks like missing words
            c.SetProperty(PropertyId.SpeechServiceResponse_ProfanityOption, "Raw");

            // Enable TrueText post-processing to remove disfluencies (uh, um), fix casing, and improve readability automatically via AI
            c.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");

            // Emit speaker tags on intermediate results (live preview)
            c.SetProperty(
                PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

            // Silence after which the current utterance is finalized and a new one begins.
            // 1200ms (up from 800ms) allows Azure a broader context window to analyze the full sentence 
            // before segmenting, significantly improving spelling and punctuation accuracy.
            c.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "1200");

            // Keep the session alive through long pauses between speakers
            c.SetProperty(
                PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "15000");

            return c;
        }

        private void PurgeExpiredJobs()
        {
            var cutoff = DateTime.UtcNow - JobTtl;
            foreach (var kv in _jobs)
            {
                if (kv.Value.CreatedAt < cutoff &&
                    kv.Value.Status is TranscriptionStatus.Completed or TranscriptionStatus.Failed)
                {
                    _jobs.TryRemove(kv.Key, out _);
                }
            }
        }
    }
}
