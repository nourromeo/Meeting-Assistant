using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using XlentWPF.Dtos;
using XlentWPF.Models;
using XlentWPF.Services.Interfaces;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;


namespace XlentWPF.Services.Implementations
{
    public class TranscriptionService : ITranscriptionService, IDisposable
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly IAudioService _audio;

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
        }

        // ── FILE UPLOAD ──────────────────────────────────────────────────────

        public async Task<IReadOnlyList<TranscriptSegment>> TranscribeFileAsync(
            string filePath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(5);

            using var http = CreateHttpClient();
            await using var fileStream = File.OpenRead(filePath);

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);

            var mimeType = Path.GetExtension(filePath).ToLower() switch
            {
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                _ => "audio/wav"
            };
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(streamContent, "file", Path.GetFileName(filePath));

            progress?.Report(20);

            var response = await http.PostAsync(
           "api/transcription/upload-fast?expectedSpeakers=3", content, cancellationToken);


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

        // ── LIVE (SignalR) ───────────────────────────────────────────────────

        public async Task StartLiveAsync()
        {
            _liveCts = new CancellationTokenSource();

            var hubUrl = $"{_baseUrl}/hubs/transcription?apiKey={Uri.EscapeDataString(_apiKey)}";

            _hub = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hub.On<TranscriptSegmentDto>("SegmentReceived", dto =>
                SegmentRecognised?.Invoke(MapSegment(dto.SpeakerId, dto.Text, dto.StartOffsetSeconds)));

            _hub.On<string>("StatusChanged", status =>
                System.Diagnostics.Debug.WriteLine($"[SignalR] {status}"));

            await _hub.StartAsync(_liveCts.Token);
            await _hub.InvokeAsync("StartLive", _liveCts.Token);

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
            _ = _hub.InvokeAsync("SendAudioChunk", chunk);
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(_baseUrl + "/") };
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            return client;
        }

        private static TranscriptSegment MapSegment(int speakerId, string text, double offsetSeconds)
        {
            var idx = Math.Clamp(speakerId, 1, 4);
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
                return Math.Clamp(direct, 1, 4);

            // "X-N" format (e.g. "Guest-1", "SPEAKER-2") — from ConversationTranscriber
            var dash = speakerId.LastIndexOf('-');
            if (dash >= 0 && int.TryParse(speakerId[(dash + 1)..], out var n))
                return Math.Clamp(n, 1, 4);

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
            StopLiveAsync().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}
