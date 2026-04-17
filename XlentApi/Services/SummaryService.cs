using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using XlentApi.Interfaces;
using XlentApi.Models;

namespace XlentApi.Services
{
    public class SummaryService : ISummaryService
    {
        private readonly AzureOpenAIClient _azureClient;
        private readonly string _deploymentName;

        public SummaryService(AzureOpenAIClient azureClient, IConfiguration config)
        {
            _azureClient = azureClient;
            _deploymentName = config["AzureOpenAI:DeploymentName"]
                ?? throw new ArgumentNullException("AzureOpenAI:DeploymentName");
        }

        public async Task<MeetingSummaryResponse> GenerateSummaryAsync(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Input text is empty.");

            var chatClient = _azureClient.GetChatClient(_deploymentName);

            var systemPrompt =
                """
                You are a meeting assistant.
                Create:
                1) A concise summary (3-8 sentences)
                2) Key points as short bullets
                3) Action items as a bullet list (imperative form, owner if possible)
                
                Rules:
                - Output MUST be valid JSON only (no markdown, no extra text).
                - JSON schema:
                  { "summary": "string", "keyPoints": ["string", "..."], "actionItems": ["string", "..."] }
                - Keep the output in the language specified by the user (or match the transcript if it is obvious).
                """;

            var userPrompt =
                $"""
                Language: {language}
                Transcript:
                {text}
                """;

            var result = await chatClient.CompleteChatAsync(new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt),
            });

            var content = result.Value?.Content?.FirstOrDefault()?.Text ?? string.Empty;

            var parsed = TryParseResponse(content);

            if (parsed == null)
            {
                return new MeetingSummaryResponse
                {
                    Summary = content.Trim(),
                    KeyPoints = new List<string>(),
                    ActionItems = new List<string>()
                };
            }

            return parsed;
        }

        private static MeetingSummaryResponse? TryParseResponse(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);

                var root = doc.RootElement;
                var summary = root.GetProperty("summary").GetString() ?? string.Empty;

                var items = new List<string>();
                var keyPoints = new List<string>();

                if (root.TryGetProperty("keyPoints", out var keyPointsElement) && keyPointsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in keyPointsElement.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            keyPoints.Add(s.Trim());
                    }
                }

                if (root.TryGetProperty("actionItems", out var actionItems) && actionItems.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in actionItems.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            items.Add(s.Trim());
                    }
                }

                return new MeetingSummaryResponse
                {
                    Summary = summary.Trim(),
                    KeyPoints = keyPoints,
                    ActionItems = items
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
