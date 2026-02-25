using System.Net.Http.Headers;
using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech;
using XlentApi.Interfaces;
using XlentApi.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddSingleton(sp =>
new AzureOpenAIClient(
        new Uri(config["AzureOpenAI:Endpoint"]),
        new Azure.AzureKeyCredential(config["AzureOpenAI:ApiKey"])));

builder.Services.AddSingleton(provider =>
{
    var configuration = SpeechConfig.FromSubscription(config["AzureSpeechToText:ApiKey"], config["AzureSpeechToText:SpeechRegion"]);
    configuration.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
    return configuration;
});

builder.Services.AddHttpClient<IFastTranscriptionService, FastTranscriptionService>(client =>
{
    client.BaseAddress = new Uri(config["AzureSpeechToText:Endpoint"]);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config["AzureSpeechToText:ApiKey"]);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<AzureOpenAiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
