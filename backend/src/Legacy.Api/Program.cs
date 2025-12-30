using System.Diagnostics;
using DotNetEnv;
using Legacy.Api;
using Legacy.Api.Data;
using Legacy.Api.DTOs;
using Legacy.Api.Filters;
using Legacy.Api.Models;
using Legacy.Api.Plugins;
using Legacy.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Data;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Qdrant.Client;
using ChatResponse = Legacy.Api.DTOs.ChatResponse;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var serviceName = "Legacy.Api";
var serviceVersion = "1.0.0";

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ");
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName, serviceVersion: serviceVersion));
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddSource("Legacy.Api.SemanticKernel")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddNpgsql()
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
    )
    .WithLogging()
    .UseOtlpExporter(OtlpExportProtocol.HttpProtobuf, new Uri("http://localhost:4318/"));

builder.Services.AddDbContext<LegacyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Configuration
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new Exception("missing OpenAI:ApiKey");
var openAiModelId = builder.Configuration["OpenAI:ModelId"] ?? "gpt-4o";

var openAiEmbeddingModelId = "text-embedding-3-small";

var qdrantHost = builder.Configuration["Qdrant:Host"] ?? "localhost";
var qdrantPort = int.Parse(builder.Configuration["Qdrant:Port"] ?? "6334");

builder.Services.AddSingleton(sp => new QdrantClient(qdrantHost, qdrantPort));
builder.Services.AddQdrantVectorStore();

// Tempo configuration for trace queries
var tempoBaseUrl = builder.Configuration["Tempo:BaseUrl"] ?? "http://localhost:3200";
builder.Services.AddHttpClient("Tempo", client =>
{
    client.BaseAddress = new Uri(tempoBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton(_ =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddOpenAIChatCompletion(openAiModelId, openAiApiKey);

#pragma warning disable SKEXP0010
    kernelBuilder.AddOpenAIEmbeddingGenerator(openAiEmbeddingModelId, openAiApiKey);
#pragma warning restore SKEXP0010

    return kernelBuilder;
});

builder.Services.AddScoped(sp =>
{
    var kernelBuilder = sp.GetRequiredService<IKernelBuilder>();
    var kernel = kernelBuilder.Build();

    var productService = sp.GetRequiredService<IProductService>();
    var orderService = sp.GetRequiredService<IOrderService>();

    kernel.Plugins.AddFromObject(new ProductsPlugin(productService), "ProductsPlugin");
    kernel.Plugins.AddFromObject(new OrdersPlugin(orderService), "OrdersPlugin");

    // Add Tempo plugin for trace queries
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var tempoHttpClient = httpClientFactory.CreateClient("Tempo");
    var tempoLogger = sp.GetRequiredService<ILogger<TempoPlugin>>();
    var tempoUrl = sp.GetRequiredService<IConfiguration>()["Tempo:BaseUrl"] ?? "http://localhost:3200";
    kernel.Plugins.AddFromObject(new TempoPlugin(tempoHttpClient, tempoUrl, tempoLogger), "TempoPlugin");

    var vectorStore = sp.GetRequiredService<QdrantVectorStore>();
    var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    var productCollection = vectorStore.GetCollection<ulong, ProductVectorRecord>("products");

#pragma warning disable SKEXP0001
    var productTextSearch = new VectorStoreTextSearch<ProductVectorRecord>(productCollection, embeddingGenerator);
#pragma warning restore SKEXP0001

    kernel.Plugins.Add(productTextSearch.CreateWithGetTextSearchResults("SearchProducts",
        "Search for products using semantic search. Use this to find products by description, name, or category."));

    // Add function invocation filter for debugging
    var filterLogger = sp.GetRequiredService<ILogger<LoggingFunctionFilter>>();
    kernel.FunctionInvocationFilters.Add(new LoggingFunctionFilter(filterLogger));

    return kernel;
});

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("=> {Method} {Path}{QueryString}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString);

    var activity = Activity.Current;
    if (activity != null)
    {
        context.Response.Headers["traceparent"] =
            $"00-{activity.TraceId}-{activity.SpanId}-{activity.ActivityTraceFlags:D}";
    }

    await next();

    stopwatch.Stop();
    logger.LogInformation("<= {Method} {Path} {StatusCode} - {ElapsedMs}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        stopwatch.ElapsedMilliseconds);
});

// app.UseHttpsRedirection();

ProductApis.Map(app);
OrderApis.Map(app);

app.MapPost("/api/chat", async (ChatRequest request, Kernel kernel, ILogger<Program> logger) =>
{
    try
    {
        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        var now = DateTimeOffset.UtcNow;
        var systemMessage =
            $"You are a helpful assistant that can help manage products and orders, and analyze application traces. Use the available functions to help the user. When asked about operations like DELETE, POST, GET requests, or errors, use the TempoPlugin to search traces.\n\nCurrent date and time: {now:yyyy-MM-dd HH:mm:ss} UTC. Current Unix timestamp: {now.ToUnixTimeSeconds()}. When querying traces, do NOT pass startTime/endTime unless the user specifies a specific time range - let the functions use their defaults.";

        var result = await kernel.InvokePromptAsync($"{systemMessage}\n\nUser: {request.Message}", new(settings),
            cancellationToken: cts.Token);

        return Results.Ok(new ChatResponse { Response = result.ToString() });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing chat request: {ex.Message}");
    }
});

var loggerProvider = app.Services.GetRequiredService<LoggerProvider>();

app.Run();

loggerProvider.ForceFlush();
