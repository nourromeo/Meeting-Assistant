using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech;
using System.Net.Http.Headers;
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
    var configuration = SpeechConfig.FromSubscription(
        config["AzureSpeechToText:ApiKey"], 
        config["AzureSpeechToText:SpeechRegion"]);
    configuration.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
    configuration.SetProperty(PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous");
    return configuration;
});

builder.Services.AddHttpClient<IFastTranscriptionService, FastTranscriptionService>(client =>
{
    client.BaseAddress = new Uri(config["AzureSpeechToText:Endpoint"]!);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config["AzureSpeechToText:ApiKey"]);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});


builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddSingleton<ITranscriptionService, TranscriptionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<TranscriptionHub>("/hubs/transcription");
app.Run();
