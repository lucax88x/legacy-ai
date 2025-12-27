using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Legacy.Api.Data;
using Legacy.Api.Models;
using Legacy.Api.DTOs;
using Legacy.Api.Services;
using Legacy.Api.Plugins;
using System.Diagnostics;
using DotNetEnv.Configuration;
using Legacy.Api;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var serviceName = "Legacy.Api";
var serviceVersion = "1.0.0";

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => { options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] "; });
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
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
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

builder.Services.AddSingleton(_ =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
    var openAiModelId = builder.Configuration["OpenAI:ModelId"] ?? "gpt-4o";

    if (string.IsNullOrEmpty(openAiApiKey))
    {
        throw new Exception("missing api-key");
    }

    kernelBuilder.AddOpenAIChatCompletion(openAiModelId, openAiApiKey);

    // to test with debezium
    // kernelBuilder.AddVectorStoreTextSearch<>()

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
        logger.LogInformation("Starting chat");

        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        var systemMessage =
            "You are a helpful assistant that can help manage products and orders. Use the available functions to help the user.";

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