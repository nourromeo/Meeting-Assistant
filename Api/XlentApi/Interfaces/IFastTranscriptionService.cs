using XlentApi.Models;

namespace XlentApi.Interfaces
{
    public interface IFastTranscriptionService
    {
        Task<FastTranscriptionResult> TranscribeAudioAsync(
            Stream audioStream,
            string fileName,
            IEnumerable<string> locale, 
            IEnumerable<string>? phraseList = null,
            int? expectedSpeakers = null);
    }
}
