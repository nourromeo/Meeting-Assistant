using XlentWPF.Models;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.Services.Mock;


//Replace with AzureTranscriptionService when ready.

public class MockTranscriptionService : ITranscriptionService
{
    private CancellationTokenSource? _cts;

    public event Action<TranscriptSegment>? SegmentRecognised;
    public event Action<string>? PartialResultReceived;

    private static readonly TranscriptSegment[] MockSegments =
    [
        new() { SpeakerName="Alex",   SpeakerIndex=1, Timestamp="00:00", Text="Good morning everyone, let's get started with today's sprint review. We have a lot to cover." },
        new() { SpeakerName="Maya",   SpeakerIndex=2, Timestamp="00:12", Text="Thanks Alex. The main items we finished this sprint were the authentication module and the new dashboard redesign." },
        new() { SpeakerName="Jordan", SpeakerIndex=3, Timestamp="00:28", Text="I can confirm the auth module is deployed to staging. We're still waiting on QA sign-off before production." },
        new() { SpeakerName="Alex",   SpeakerIndex=1, Timestamp="00:41", Text="Great. What's the timeline on QA? Can we push to production by end of week?" },
        new() { SpeakerName="Maya",   SpeakerIndex=2, Timestamp="00:51", Text="That depends on the test results. Sarah estimated two more days for the security pass. Friday looks realistic." },
    ];

    public Task StartLiveAsync()
    {
        _cts = new CancellationTokenSource();
        _ = StreamSegmentsAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public Task StopLiveAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TranscriptSegment>> TranscribeFileAsync(
        string filePath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Simulate file processing time with progress reporting
        for (int i = 0; i <= 100; i += 5)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(i);
            await Task.Delay(60, cancellationToken);
        }

        return MockSegments;
    }

    private async Task StreamSegmentsAsync(CancellationToken ct)
    {
        foreach (var segment in MockSegments)
        {
            if (ct.IsCancellationRequested) break;

            // Simulate partial result first
            PartialResultReceived?.Invoke(segment.Text[..Math.Min(20, segment.Text.Length)] + "…");
            await Task.Delay(800, ct).ContinueWith(_ => { });

            if (ct.IsCancellationRequested) break;

            // Then fire the final recognised segment
            SegmentRecognised?.Invoke(segment);
            await Task.Delay(1800, ct).ContinueWith(_ => { });
        }
    }

    public void Dispose() => _cts?.Dispose();
}
