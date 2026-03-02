using XlentWPF.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace  XlentWPF.Services.Interfaces;

/// <summary>
/// Abstracts speech-to-text for both live microphone and file upload.
/// Real implementation will use Azure Cognitive Services Speech SDK.
/// </summary>
public interface ITranscriptionService : IDisposable
{
    /// <summary>
    /// Fired each time a complete utterance is recognised.
    /// Provides the recognised segment including speaker and timestamp.
    /// </summary>
    event Action<TranscriptSegment> SegmentRecognised;

    /// <summary>
    /// Fired when a partial (interim) result is available — useful for
    /// showing live "in-progress" text before the utterance is finalised.
    /// </summary>
    event Action<string> PartialResultReceived;

    /// <summary>Start continuous live recognition from the microphone.</summary>
    Task StartLiveAsync();

    /// <summary>Stop continuous live recognition.</summary>
    Task StopLiveAsync();

    /// <summary>
    /// Transcribe an audio file and return all segments.
    /// Reports progress 0–100 via the optional callback.
    /// </summary>
    Task<IReadOnlyList<TranscriptSegment>> TranscribeFileAsync(
        string filePath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

