using Microsoft.AspNetCore.SignalR;
using XlentApi.Interfaces;

namespace XlentApi.Hubs
{
    public class TranscriptionHub : Hub
    {
        private readonly ITranscriptionService _transcription;
        private readonly IHubContext<TranscriptionHub> _hubContext;
        private readonly ILogger<TranscriptionHub> _logger;


        public TranscriptionHub(
            ITranscriptionService transcription,
            IHubContext<TranscriptionHub> hubContext,
            ILogger<TranscriptionHub> logger)
        {
            _transcription = transcription;
            _hubContext = hubContext;
            _logger = logger;
        }

        // language: optional BCP-47 tag (e.g. "sv-SE", "en-US").
        // Defaults to whatever is configured in appsettings when null.
        public async Task StartLive(string? language = null)
        {
            var connectionId = Context.ConnectionId;
            var hubContext = _hubContext;

            await _transcription.CreateRealTimeSessionAsync(
                sessionId: connectionId,
                language: language,

                onSegmentRecognized: async segment =>
                    await hubContext.Clients.Client(connectionId)
                                            .SendAsync("SegmentReceived", segment),

                onPartialResult: async text =>
                    await hubContext.Clients.Client(connectionId)
                                            .SendAsync("PartialResultReceived", text),

                onError: async message =>
                {
                    _logger.LogError("Speech SDK error [{ConnectionId}]: {Message}", connectionId, message);
                    await hubContext.Clients.Client(connectionId)
                                            .SendAsync("StatusChanged", $"Error: {message}");
                });

            await Clients.Caller.SendAsync("StatusChanged", "Live session started");
        }

        public async Task SendAudioChunk(byte[] audioChunk)
        {
            if (audioChunk == null || audioChunk.Length == 0) return;
            await _transcription.PushAudioChunkAsync(Context.ConnectionId, audioChunk);
        }

        public async Task StopLive()
        {
            await _transcription.StopRealTimeSessionAsync(Context.ConnectionId);
            await Clients.Caller.SendAsync("StatusChanged", "Live session ended");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try { await _transcription.StopRealTimeSessionAsync(Context.ConnectionId); }
            catch { /* session may already be closed */ }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
