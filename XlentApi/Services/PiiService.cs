using System.Text.RegularExpressions;
using Azure;
using Azure.AI.TextAnalytics;
using XlentApi.Interfaces;
using XlentApi.Models;

namespace XlentApi.Services
{
    public class PiiService : IPiiService
    {
        private readonly TextAnalyticsClient _textAnalyticsClient;
        private readonly ILogger<PiiService> _logger;

        // Strict 250ms timeout threshold to prevent Regular Expression Denial of Service (ReDoS)
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        private static readonly Regex PnrRegex = new Regex(
            @"\b(?:19|20)?\d{2}(?:0[1-9]|1[0-2])(?:0[1-9]|[12]\d|3[01]|6[1-9]|[78]\d|9[01])[-+]?\d{4}\b",
            RegexOptions.Compiled, RegexTimeout);

        private static readonly Regex EmailRegex = new Regex(
            @"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b",
            RegexOptions.Compiled, RegexTimeout);

        private static readonly Regex PhoneRegex = new Regex(
            @"(?<!\w)(?:(?:\+|00)\s?46|0)[\s-]?(?:\(?\d{1,3}\)?[\s-]?)?(?:\d[\s.-]?){5,8}\d(?!\w)",
            RegexOptions.Compiled, RegexTimeout);

        public PiiService(TextAnalyticsClient textAnalyticsClient, ILogger<PiiService> logger)
        {
            _textAnalyticsClient = textAnalyticsClient;
            _logger = logger;
        }

        public async Task<List<TranscriptSegment>> RedactTranscriptAsync(IEnumerable<TranscriptSegment> segments, string language, PiiSettings? piiSettings, CancellationToken cancellationToken = default)
        {
            piiSettings ??= new PiiSettings();
            var redactedSegments = new List<TranscriptSegment>();
            var batchDocuments = new List<TextDocumentInput>();

            var preservedTokens = new Dictionary<string, string>();
            int index = 0;

            foreach (var segment in segments)
            {
                var regexRedactedText = ApplyDeterministicRedaction(segment.Text, piiSettings, preservedTokens);

                redactedSegments.Add(new TranscriptSegment
                {
                    Text = regexRedactedText,
                    SpeakerId = segment.SpeakerId,
                    StartOffsetSeconds = segment.StartOffsetSeconds,
                    DurationSeconds = segment.DurationSeconds,
                    IsFinal = segment.IsFinal
                });

                // Use the list index as a temporary ID for Azure Text Analytics
                batchDocuments.Add(new TextDocumentInput(index.ToString(), regexRedactedText)
                {
                    Language = language
                });

                index++;
            }

            // Process in chunks of 100 to respect Azure Cognitive Services API payload limits
            var chunkedBatches = batchDocuments.Chunk(5);

            foreach (var batch in chunkedBatches)
            {
                var options = new RecognizePiiEntitiesOptions();

                if (piiSettings.RedactNames) options.CategoriesFilter.Add(PiiEntityCategory.Person);
                if (piiSettings.RedactAddress) options.CategoriesFilter.Add(PiiEntityCategory.Address);
                if (piiSettings.RedactOrganizations) options.CategoriesFilter.Add(PiiEntityCategory.Organization);
                if (piiSettings.RedactEmails) options.CategoriesFilter.Add(PiiEntityCategory.Email);
                if (piiSettings.RedactPhoneNumbers) options.CategoriesFilter.Add(PiiEntityCategory.PhoneNumber);

                if (options.CategoriesFilter.Count == 0) continue;

                try
                {
                    var response = await _textAnalyticsClient.RecognizePiiEntitiesBatchAsync(batch, options, cancellationToken);

                    foreach (var docResult in response.Value)
                    {
                        // Parse the ID back to an integer to find the exact segment in our list
                        if (int.TryParse(docResult.Id, out int segmentIndex))
                        {
                            var matchingSegment = redactedSegments[segmentIndex];

                            if (!docResult.HasError)
                            {
                                matchingSegment.Text = ApplyEntityMasking(matchingSegment.Text, docResult.Entities);
                            }
                            else
                            {
                                _logger.LogWarning("Azure AI Language error for segment {Id}: {Error}", docResult.Id, docResult.Error.Message);
                            }
                        }
                    }
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, "Azure AI Language Service request failed entirely.");
                }
            }

            foreach (var segment in redactedSegments)
            {
                foreach (var kvp in preservedTokens)
                {
                    if (segment.Text.Contains(kvp.Key))
                    {
                        segment.Text = segment.Text.Replace(kvp.Key, kvp.Value);
                    }
                }
            }

            return redactedSegments;
        }

        private string ApplyDeterministicRedaction(string text, PiiSettings piiSettings, Dictionary<string, string> preservedTokens)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;


            try
            {
                if (piiSettings.RedactPersonnummer)
                {
                    text = PnrRegex.Replace(text, "[Personnummer]");
                }
                else
                {
                    text = PnrRegex.Replace(text, match =>
                    {
                        var token = $"__PNR_SHIELD_{Guid.NewGuid():N}__";
                        preservedTokens[token] = match.Value;
                        return token;
                    });
                }

                if (piiSettings.RedactEmails) text = EmailRegex.Replace(text, "[E-post]");
                if (piiSettings.RedactPhoneNumbers) text = PhoneRegex.Replace(text, "[Telefonnummer]");

            }
            catch (RegexMatchTimeoutException ex)
            {
                _logger.LogWarning(ex, "Regex execution timed out.");
            }

            return text;
        }

        private string ApplyEntityMasking(string originalText, PiiEntityCollection entities)
        {
            if (entities.Count == 0) return originalText;

            // Sort by Offset DESCENDING so string length changes don't mess up subsequent replacements
            var sortedEntities = entities.OrderByDescending(e => e.Offset).ToList();
            var result = originalText;

            foreach (var entity in sortedEntities)
            {
                var replacementLabel = entity.Category.ToString() switch
                {
                    "Person" => "[Person]",
                    "Organization" => "[Organisation]",
                    "PhoneNumber" => "[Telefonnummer]",
                    "Email" => "[E-post]",
                    "Address" => "[Adress]",
                    _ => $"[{entity.Category.ToString()}]"
                };
                result = result.Remove(entity.Offset, entity.Length)
                               .Insert(entity.Offset, replacementLabel);
            }

            return result;
        }
    }
}
