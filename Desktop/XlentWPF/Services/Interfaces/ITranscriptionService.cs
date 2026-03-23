using XlentWPF.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace  XlentWPF.Services.Interfaces;


public interface ITranscriptionService : IDisposable
{
 
    event Action<TranscriptSegment> SegmentRecognised;

  
    event Action<string> PartialResultReceived;

    
    Task StartLiveAsync();

    
    Task StopLiveAsync();

    Task<IReadOnlyList<TranscriptSegment>> TranscribeFileAsync(
        string filePath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

