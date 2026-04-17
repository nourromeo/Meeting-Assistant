using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.TextAnalytics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Options;
using XlentApi.Hubs;
using XlentApi.Interfaces;
using XlentApi.API.Middleware;
using XlentApi.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Bind and validate configuration into POCOs (fail-fast on missing required values)
builder.Services.AddOptions<AzureOpenAIOptions>()
    .Bind(config.GetSection("AzureOpenAI"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AzureSpeechToTextOptions>()
    .Bind(config.GetSection("AzureSpeechToText"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AzureTextPiiRedactionOptions>()
    .Bind(config.GetSection("AzureTextPIIRedaction"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Azure OpenAI client (uses validated options)
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
    return new AzureOpenAIClient(new Uri(opts.Endpoint), new AzureKeyCredential(opts.ApiKey));
});

// Speech config registration (uses validated options)
builder.Services.AddSingleton(provider =>
{
    var opts = provider.GetRequiredService<IOptions<AzureSpeechToTextOptions>>().Value;

    var speechConfig = SpeechConfig.FromSubscription(opts.ApiKey, opts.SpeechRegion);
    speechConfig.SpeechRecognitionLanguage = opts.Language ?? "sv-SE";

    speechConfig.SetProperty(PropertyId.SpeechServiceResponse_ProfanityOption, "Raw");
    speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
    speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "800");
    speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "15000");

    return speechConfig;
});

// Fast transcription HttpClient using validated options
builder.Services.AddHttpClient<IFastTranscriptionService, FastTranscriptionService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<AzureSpeechToTextOptions>>().Value;
    client.BaseAddress = new Uri(opts.Endpoint);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", opts.ApiKey);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Text Analytics client for PII redaction (uses validated options)
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AzureTextPiiRedactionOptions>>().Value;
    var endpoint = new Uri(opts.Endpoint);
    var credential = new AzureKeyCredential(opts.ApiKey);
    return new TextAnalyticsClient(endpoint, credential);
});

builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddSingleton<ITranscriptionService, TranscriptionService>();
builder.Services.AddSingleton<IPiiService, PiiService>();

var app = builder.Build();

app.UseHttpsRedirection();
var appApiKey =
    config["ApiSecurity:ApiKey"] ??
    config["ApiSettings:ApiKey"] ??
    config["ApiKey"];

if (!string.IsNullOrWhiteSpace(appApiKey))
{
    app.UseMiddleware<ApiKeyMiddleware>();
}
else if (app.Environment.IsDevelopment())
{
    app.Logger.LogWarning(
        "No app API key configured (ApiSecurity:ApiKey / ApiSettings:ApiKey / ApiKey). " +
        "API key middleware is disabled in Development.");
}
else
{
    throw new InvalidOperationException(
        "Missing API key configuration for non-development environment. " +
        "Set ApiSecurity:ApiKey, ApiSettings:ApiKey, or ApiKey.");
}
app.UseAuthorization();
app.MapControllers();
app.MapHub<TranscriptionHub>("/hubs/transcription");
app.Run();

// --- Options classes used for binding and validation ---
public class AzureOpenAIOptions
{
    [Required]
    public string Endpoint { get; set; } = default!;
    [Required]
    public string ApiKey { get; set; } = default!;
    public string? DeploymentName { get; set; }
}

public class AzureSpeechToTextOptions
{
    [Required]
    public string Endpoint { get; set; } = default!;
    [Required]
    public string ApiKey { get; set; } = default!;
    [Required]
    public string SpeechRegion { get; set; } = default!;
    public string? Language { get; set; }
}

public class AzureTextPiiRedactionOptions
{
    [Required]
    public string Endpoint { get; set; } = default!;
    [Required]
    public string ApiKey { get; set; } = default!;
}
