using System.Diagnostics;
using Microsoft.SemanticKernel;

namespace Legacy.Api.Filters;

public class LoggingFunctionFilter(ILogger<LoggingFunctionFilter> logger) : IFunctionInvocationFilter
{
    private static readonly ActivitySource ActivitySource = new("Legacy.Api.SemanticKernel");

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var pluginName = context.Function.PluginName ?? "Unknown";
        var functionName = context.Function.Name;

        using var activity = ActivitySource.StartActivity(
            $"{pluginName}.{functionName}");

        activity?.SetTag("sk.plugin.name", pluginName);
        activity?.SetTag("sk.function.name", functionName);

        foreach (var arg in context.Arguments)
        {
            activity?.SetTag($"sk.argument.{arg.Key}", arg.Value?.ToString());
        }

        logger.LogInformation("[SK] Invoking: {Plugin}.{Function} with args: {Args}",
            pluginName,
            functionName,
            string.Join(", ", context.Arguments.Select(a => $"{a.Key}={a.Value}")));

        try
        {
            await next(context);

            activity?.SetStatus(ActivityStatusCode.Ok);

            var resultPreview = context.Result.ToString();
            if (resultPreview?.Length > 200)
                resultPreview = resultPreview[..200] + "...";

            activity?.SetTag("sk.result.preview", resultPreview);

            logger.LogInformation("[SK] Completed: {Plugin}.{Function} => {Result}",
                pluginName,
                functionName,
                resultPreview);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("sk.error", ex.Message);
            throw;
        }
    }
}