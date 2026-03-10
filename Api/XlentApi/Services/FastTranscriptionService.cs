using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public async Task<FastTranscriptionResult> TranscribeAudioAsync(
            Stream audioStream, 
            string fileName,
            IEnumerable<string> locale,
            IEnumerable<string>? phraseList = null,
            int? expectedSpeakers = null)
        {
            object diarizationSettings;

            if (expectedSpeakers.HasValue && expectedSpeakers > 0)
            {
                diarizationSettings = new
                {
                    enabled = true,
                    minSpeakers = expectedSpeakers.Value,
                    maxSpeakers = expectedSpeakers.Value,
                };
            }
            else
            {
                diarizationSettings = new
                {
                    enabled = true
                };
            }

            var definitionPayload = new
            {
                locales = locale.ToArray(),
                diarization = diarizationSettings,
                profanityFilterMode = "None",

                phraseList = phraseList != null && phraseList.Any() ? new { phrases = phraseList.ToArray() } : null
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var definitionJson = JsonSerializer.Serialize(definitionPayload, jsonOptions);

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
                        Text = phrase.Text ?? string.Empty,
                        OffsetMilliseconds = phrase.OffsetMilliseconds,
                        DurationMilliseconds = phrase.DurationMilliseconds
                    });
                }
            }

            return result;
        }
    }
}
