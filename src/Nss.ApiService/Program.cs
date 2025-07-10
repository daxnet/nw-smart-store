using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Nss.ApiService.Services;
using Qdrant.Client;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var qdrantUri = builder.Configuration["nss:qdrant:uri"] ?? throw new InvalidOperationException("Qdrant URI is not configured.");
var chatAiModelDeployment = builder.Configuration["nss:ai:chat:deployment"] ?? throw new InvalidOperationException("Chat AI model deployment is not configured.");
var chatAiModelEndpoint = builder.Configuration["nss:ai:chat:endpoint"] ?? throw new InvalidOperationException("Chat AI model endpoint is not configured.");
var chatAiModelApiKey = builder.Configuration["nss:ai:chat:apikey"] ?? throw new InvalidOperationException("Chat AI model API key is not configured.");
var embeddingAiModelDeployment = builder.Configuration["nss:ai:embedding:deployment"] ?? throw new InvalidOperationException("Embedding AI model deployment is not configured.");
var embeddingAiModelEndpoint = builder.Configuration["nss:ai:embedding:endpoint"] ?? throw new InvalidOperationException("Embedding AI model endpoint is not configured.");
var embeddingAiModelApiKey = builder.Configuration["nss:ai:embedding:apikey"] ?? throw new InvalidOperationException("Embedding AI model API key is not configured.");

Log.Logger = new LoggerConfiguration()
    .ReadFrom
    .Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddControllers(config =>
{
    config.SuppressAsyncSuffixInActionNames = false;
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});


builder.Services
    .AddOpenApi()
    .AddLogging(config =>
    {
        config.ClearProviders();
        config.AddSerilog(Log.Logger, true);
    })
    .AddRouting(options =>
    {
        options.LowercaseQueryStrings = true;
        options.LowercaseUrls = true;
    })
    .AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", pb =>
        {
            pb.AllowAnyHeader();
            pb.AllowAnyMethod();
            pb.AllowAnyOrigin();
        });
    })
    .AddSingleton(_ => new QdrantClient(qdrantUri))
    .AddSingleton(_ =>
    {
#pragma warning disable SKEXP0010
        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(chatAiModelDeployment, chatAiModelEndpoint, chatAiModelApiKey)
            .AddAzureOpenAIEmbeddingGenerator(embeddingAiModelDeployment, embeddingAiModelEndpoint,
                embeddingAiModelApiKey)
#pragma warning restore SKEXP0010
            .Build();
        return kernel;
    })
    .AddSingleton(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    })
    .AddSingleton<QdrantIndexService>()
    .AddSingleton<SearchService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(config =>
    {
        config.Title = "Northwind Smart Store Search API";
    });
}

app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();