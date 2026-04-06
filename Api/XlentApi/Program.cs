using System.Net.Http.Headers;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.TextAnalytics;
using Microsoft.CognitiveServices.Speech;
using XlentApi.Hubs;
using XlentApi.Interfaces;
using XlentApi.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton(sp =>
    new AzureOpenAIClient(
        new Uri(config["AzureOpenAI:Endpoint"]!),
        new Azure.AzureKeyCredential(config["AzureOpenAI:ApiKey"]!)));

builder.Services.AddSingleton(provider =>
{
    var speechConfig = SpeechConfig.FromSubscription(
        config["AzureSpeechToText:ApiKey"],
        config["AzureSpeechToText:SpeechRegion"]);

    // Default language — overridden per session by AutoDetect
    speechConfig.SpeechRecognitionLanguage =
        config["AzureSpeechToText:Language"] ?? "sv-SE";

    // Return raw text — never mask or remove words with profanity filter
    speechConfig.SetProperty(
        PropertyId.SpeechServiceResponse_ProfanityOption, "Raw");

    // Intermediate speaker diarization during recognition
    speechConfig.SetProperty(
        PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

    // How long silence must last before an utterance is finalized (800ms is
    // a good balance: responsive without cutting off mid-sentence pauses)
    speechConfig.SetProperty(
        PropertyId.Speech_SegmentationSilenceTimeoutMs, "800");

    // How long the SDK waits with no speech before ending the session.
    // Keep high so long pauses between speakers don't close the session.
    speechConfig.SetProperty(
        PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "15000");

    return speechConfig;
});

builder.Services.AddHttpClient<IFastTranscriptionService, FastTranscriptionService>(client =>
{
    client.BaseAddress = new Uri(config["AzureSpeechToText:Endpoint"]!);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config["AzureSpeechToText:ApiKey"]);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton(sp =>
{
    var endpoint = new Uri(config["AzureTextPIIRedaction:Endpoint"]!);
    var credential = new AzureKeyCredential(config["AzureTextPIIRedaction:ApiKey"]!);
    return new TextAnalyticsClient(endpoint, credential);
});

builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddSingleton<ITranscriptionService, TranscriptionService>();
builder.Services.AddSingleton<IPiiService, PiiService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TranscriptionHub>("/hubs/transcription");
app.Run();
