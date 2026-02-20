using Azure.AI.OpenAI;
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
