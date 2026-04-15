using XlentWPF.Models;

namespace XlentWPF.Services.Interfaces;

public interface ITranscriptionService : IDisposable
{
    event Action<TranscriptSegment> SegmentRecognised;
    event Action<string> PartialResultReceived;

    Task StartLiveAsync(string? language = null);
    Task StopLiveAsync();

    Task<IReadOnlyList<TranscriptSegment>> TranscribeFileAsync(
        string filePath,
        int expectedSpeakers = 2,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TranscriptSegment>> RedactAsync(
        IReadOnlyList<TranscriptSegment> segments,
        string language = "sv",
        PiiSettings? settings = null,
        CancellationToken cancellationToken = default);
}
