using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using XlentWPF.Dtos;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;


namespace XlentWPF.Services.Implementations
{
    public class TranscriptionService : ITranscriptionService, IDisposable
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly IAudioService _audio;
        // Single shared HttpClient — avoids socket exhaustion from per-request instances
        private readonly HttpClient _http;

        private HubConnection? _hub;
        private CancellationTokenSource? _liveCts;
        private bool _disposed;

        public event Action<TranscriptSegment>? SegmentRecognised;
        public event Action<string>? PartialResultReceived;

        public TranscriptionService(IConfiguration configuration, IAudioService audio)
        {
            _baseUrl = (configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001/").TrimEnd('/');
            _apiKey = configuration["ApiSettings:ApiKey"] ?? string.Empty;
            _audio = audio;

            _http = new HttpClient { BaseAddress = new Uri(_baseUrl + "/") };
            _http.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
        }

        // ── FILE UPLOAD ──────────────────────────────────────────────────────

        public async Task<IReadOnlyList<TranscriptSegment>> TranscribeFileAsync(
            string filePath,
            int expectedSpeakers = 2,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(5);

            await using var fileStream = File.OpenRead(filePath);

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);

            var mimeType = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                _ => "audio/wav"
            };
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(streamContent, "file", Path.GetFileName(filePath));

            progress?.Report(20);

            var response = await _http.PostAsync(
                $"api/transcription/upload-fast?expectedSpeakers={expectedSpeakers}",
                content,
                cancellationToken);

            progress?.Report(70);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<FastTranscriptionResultDto>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Empty response from transcription API.");

            progress?.Report(100);

            return result.Segments
                .Select(s => MapSegment(s.SpeakerId ?? 1, s.Text, s.StartOffsetSeconds))
                .ToList();
        }

        // ── PII REDACTION ────────────────────────────────────────────────────

        public async Task<IReadOnlyList<TranscriptSegment>> RedactAsync(
            IReadOnlyList<TranscriptSegment> segments,
            string language = "sv",
            PiiSettings? settings = null,
            CancellationToken cancellationToken = default)
        {
            var requestDto = new RedactionRequestDto
            {
                Language = language,
                Settings = settings ?? new PiiSettings(),
                Transcripts = segments.Select(s => new TranscriptSegmentDto
                {
                    Text = s.Text,
                    SpeakerId = s.SpeakerName,
                    StartOffsetSeconds = 0,
                    DurationSeconds = 0,
                    IsFinal = true
                }).ToList()
            };

            var response = await _http.PostAsJsonAsync(
                "api/transcription/redact", requestDto, cancellationToken);
            response.EnsureSuccessStatusCode();

            var redacted = await response.Content
                .ReadFromJsonAsync<List<TranscriptSegment>>(cancellationToken: cancellationToken)
                ?? [];

            return redacted;
        }

        // ── LIVE (SignalR) ───────────────────────────────────────────────────

        public async Task StartLiveAsync(string? language = null)
        {
            _liveCts = new CancellationTokenSource();

            // API key passed via AccessTokenProvider (Bearer header) — not in the URL query string
            var hubUrl = $"{_baseUrl}/hubs/transcription";
            var apiKey = _apiKey;

            _hub = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(apiKey);
                })
                .WithAutomaticReconnect()
                .Build();

            // Final recognized segment — add to transcript
            _hub.On<TranscriptSegmentDto>("SegmentReceived", dto =>
                SegmentRecognised?.Invoke(MapSegment(dto.SpeakerId, dto.Text, dto.StartOffsetSeconds)));

            // Intermediate/partial text — shown as live preview while speaking
            _hub.On<string>("PartialResultReceived", text =>
                PartialResultReceived?.Invoke(text));

            // Status/error messages — surface to Debug output
            _hub.On<string>("StatusChanged", status =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] {status}");
                if (status.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                    PartialResultReceived?.Invoke($"[{status}]");
            });

            await _hub.StartAsync(_liveCts.Token);
            await _hub.InvokeAsync("StartLive", language, _liveCts.Token);

            _audio.AudioDataAvailable += ForwardAudioChunk;
        }

        public async Task StopLiveAsync()
        {
            _audio.AudioDataAvailable -= ForwardAudioChunk;

            if (_hub is not null)
            {
                try { await _hub.InvokeAsync("StopLive"); } catch { /* best effort */ }
                await _hub.StopAsync();
                await _hub.DisposeAsync();
                _hub = null;
            }

            _liveCts?.Cancel();
            _liveCts?.Dispose();
            _liveCts = null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ForwardAudioChunk(byte[] chunk)
        {
            if (_hub?.State != HubConnectionState.Connected) return;
            _ = _hub.InvokeAsync("SendAudioChunk", chunk)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            System.Diagnostics.Debug.WriteLine(
                                $"[Audio] Chunk send failed: {t.Exception?.GetBaseException().Message}");
                    }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static TranscriptSegment MapSegment(int speakerId, string text, double offsetSeconds)
        {
            var idx = Math.Clamp(speakerId, 1, 8);
            return new TranscriptSegment
            {
                SpeakerName = $"Person {idx}",
                SpeakerIndex = idx,
                Timestamp = FormatTimestamp(offsetSeconds),
                Text = text
            };
        }

        private static TranscriptSegment MapSegment(string speakerId, string text, double offsetSeconds)
        {
            var idx = ParseSpeakerIndex(speakerId);
            return new TranscriptSegment
            {
                SpeakerName = $"Person {idx}",
                SpeakerIndex = idx,
                Timestamp = FormatTimestamp(offsetSeconds),
                Text = text
            };
        }

        private static int ParseSpeakerIndex(string speakerId)
        {
            // Plain integer (e.g. "1", "2") — from batch/fast transcription
            if (int.TryParse(speakerId, out var direct))
                return Math.Clamp(direct, 1, 8);

            // "X-N" format (e.g. "Guest-1", "SPEAKER-2") — from ConversationTranscriber
            var dash = speakerId.LastIndexOf('-');
            if (dash >= 0 && int.TryParse(speakerId[(dash + 1)..], out var n))
                return Math.Clamp(n, 1, 8);

            return 1;
        }

        private static string FormatTimestamp(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"mm\:ss");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Do not block with .GetResult() — fire-and-forget cleanup to avoid UI thread deadlock
            _ = StopLiveAsync();
            _http.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
