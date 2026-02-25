using System.Net.Http.Headers;
using System.Text.Json;
using XlentApi.Interfaces;
using XlentApi.Models;

namespace XlentApi.Services
{
    public class FastTranscriptionService : IFastTranscriptionService
    {
        private readonly HttpClient httpClient;

        public FastTranscriptionService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
        public async Task<FastTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string locale = "sv-SE")
        {
            var definitionPayload = new
            {
                locales = new[] { locale },
                diarization = new
                {
                    enabled = true,
                    minSpeakers = 1,
                    maxSpeakers = 3
                },
                profanityFilterMode = "None"
            };

            var definitionJson = JsonSerializer.Serialize(definitionPayload);

            using var requestContent = new MultipartFormDataContent();

            var fileContent = new StreamContent(audioStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            requestContent.Add(fileContent, "audio", fileName);

            var definitionContent = new StringContent(definitionJson);
            definitionContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requestContent.Add(definitionContent, "definition");

            var response = await httpClient.PostAsync("speechtotext/transcriptions:transcribe?api-version=2024-11-15", requestContent);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var azureData = JsonSerializer.Deserialize<AzureFastTranscriptionResponse>(jsonResponse, options);

            var result = new FastTranscriptionResult
            {
                FullText = azureData?.CombinedPhrases?[0]?.Text ?? string.Empty
            };

            if (azureData?.Phrases != null)
            {
                foreach (var phrase in azureData.Phrases)
                {
                    //var rawSpeaker = phrase.Speaker?.ToString();
                    //var speakerId = string.IsNullOrEmpty(rawSpeaker) ? "Unknown" : rawSpeaker;

                    //if (!result.Speakers.ContainsKey(speakerId))
                    //{
                    //    result.Speakers[speakerId] = speakerId == "Unknown" ? "Unknown Speaker" : $"Speaker {speakerId}";
                    //}

                    result.Segments.Add(new FastTranscriptSegment
                    {
                        SpeakerId = phrase.Speaker,
                        Text = phrase.Text,
                        OffsetMilliseconds = phrase.OffsetMilliseconds,
                        DurationMilliseconds = phrase.DurationMilliseconds
                    });
                }
            }

            return result;
        }
    }
}
