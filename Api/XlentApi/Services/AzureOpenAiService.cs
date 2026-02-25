using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;


namespace XlentApi.Services
{
    public class AzureOpenAiService
    {
        private readonly AzureOpenAIClient azureClient;
        private readonly string deploymentName;

        public AzureOpenAiService(AzureOpenAIClient azureClient, IConfiguration config)
        {
            this.azureClient = azureClient;
            deploymentName = config["AzureOpenAI:DeploymentName"] ?? throw new ArgumentNullException("AzureOpenAI:Deployment");
        }

        public async Task TestApi()
        {
            ChatClient chatClient = azureClient.GetChatClient(deploymentName);

            var clientResult = await chatClient.CompleteChatAsync(new ChatMessage[]
            {
                new SystemChatMessage("You are a helpful assistant that talks like a pirate."),
                new UserChatMessage("Hi, can you help me?"),
                new AssistantChatMessage("Arrr! Of course, me hearty! What can I do for ye?"),
                new UserChatMessage("What's the best way to train a parrot?")
            });

            var completion = clientResult.Value;

            Console.WriteLine($"Model: {completion.Model}");
            Console.WriteLine($"Role: {completion.Role}");
            Console.WriteLine($"Finish reason: {completion.FinishReason}");
            if (completion.Content != null && completion.Content.Count > 0)
            {
                Console.WriteLine("Message:");
                Console.WriteLine(completion.Content[0].Text);
            }
            else
            {
                Console.WriteLine("No content returned.");
            }
        }
    }
}
