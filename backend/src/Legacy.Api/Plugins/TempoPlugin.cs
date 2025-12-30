using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

namespace Legacy.Api.Plugins;

public class TempoPlugin(HttpClient httpClient, string tempoBaseUrl, ILogger<TempoPlugin> logger)
{
  private readonly HttpClient _httpClient = httpClient;
  private readonly string _tempoBaseUrl = tempoBaseUrl.TrimEnd('/');
  private readonly ILogger<TempoPlugin> _logger = logger;

  [KernelFunction]
  [Description("Search for traces using TraceQL query. Use this to find traces by HTTP method, status code, service name, or other attributes. Examples: 'span.http.request.method=\"DELETE\"' finds all DELETE requests, 'status=error' finds error traces, 'resource.service.name=\"Legacy.Api\"' finds traces from a specific service.")]
  public async Task<string> SearchTraces(
      [Description("TraceQL query without braces (e.g., 'span.http.request.method=\"DELETE\"', 'status=error', 'resource.service.name=\"Legacy.Api\"')")]
        string query,
      [Description("Maximum number of traces to return (default: 20)")]
        int limit = 20,
      [Description("Start time in Unix epoch seconds (optional, defaults to last hour)")]
        long? startTime = null,
      [Description("End time in Unix epoch seconds (optional, defaults to now)")]
        long? endTime = null)
  {
    try
    {
      var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      var start = startTime ?? now - 3600; // Default: last hour
      var end = endTime ?? now;

      var normalizedQuery = NormalizeTraceQLQuery(query);
      var url = $"{_tempoBaseUrl}/api/search?q={Uri.EscapeDataString(normalizedQuery)}&limit={limit}&start={start}&end={end}";

      _logger.LogInformation("Querying Tempo: {Url}", url);

      var response = await _httpClient.GetAsync(url);

      if (!response.IsSuccessStatusCode)
      {
        var error = await response.Content.ReadAsStringAsync();
        return $"Error querying Tempo: {response.StatusCode} - {error}";
      }

      var content = await response.Content.ReadAsStringAsync();
      var result = JsonSerializer.Deserialize<TempoSearchResponse>(content);

      if (result?.Traces == null || result.Traces.Count == 0)
      {
        return $"No traces found matching query: {query}";
      }

      var summary = result.Traces.Select(t => new
      {
        TraceId = t.TraceId,
        RootServiceName = t.RootServiceName,
        RootTraceName = t.RootTraceName,
        StartTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(t.StartTimeUnixNano) / 1_000_000).ToString("yyyy-MM-dd HH:mm:ss"),
        DurationMs = t.DurationMs
      });

      return JsonSerializer.Serialize(new
      {
        TotalTraces = result.Traces.Count,
        Query = query,
        Traces = summary
      }, new JsonSerializerOptions { WriteIndented = true });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error searching traces");
      return $"Error searching traces: {ex.Message}";
    }
  }

  [KernelFunction]
  [Description("Count traces matching a TraceQL query. Use this to get statistics like 'how many DELETE operations were performed' or 'how many errors occurred'.")]
  public async Task<string> CountTraces(
      [Description("TraceQL query without braces (e.g., 'span.http.request.method=\"DELETE\"', 'status=error')")]
        string query,
      [Description("Start time in Unix epoch seconds (optional, defaults to last hour)")]
        long? startTime = null,
      [Description("End time in Unix epoch seconds (optional, defaults to now)")]
        long? endTime = null)
  {
    try
    {
      var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      var start = startTime ?? now - 3600;
      var end = endTime ?? now;

      var normalizedQuery = NormalizeTraceQLQuery(query);
      var url = $"{_tempoBaseUrl}/api/search?q={Uri.EscapeDataString(normalizedQuery)}&limit=1000&start={start}&end={end}";

      _logger.LogInformation("CountTraces - Original query: '{OriginalQuery}', Normalized: '{NormalizedQuery}'", query, normalizedQuery);
      _logger.LogInformation("Counting traces in Tempo: {Url}", url);

      var response = await _httpClient.GetAsync(url);

      if (!response.IsSuccessStatusCode)
      {
        var error = await response.Content.ReadAsStringAsync();
        return $"Error querying Tempo: {response.StatusCode} - {error}";
      }

      var content = await response.Content.ReadAsStringAsync();
      var result = JsonSerializer.Deserialize<TempoSearchResponse>(content);

      var count = result?.Traces?.Count ?? 0;
      var timeRange = $"{DateTimeOffset.FromUnixTimeSeconds(start):yyyy-MM-dd HH:mm} to {DateTimeOffset.FromUnixTimeSeconds(end):yyyy-MM-dd HH:mm}";

      return $"Found {count} traces matching '{query}' in the time range {timeRange}";
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error counting traces");
      return $"Error counting traces: {ex.Message}";
    }
  }

  [KernelFunction]
  [Description("Get details of a specific trace by its trace ID.")]
  public async Task<string> GetTrace(
      [Description("The trace ID to retrieve")]
        string traceId)
  {
    try
    {
      var url = $"{_tempoBaseUrl}/api/traces/{traceId}";

      _logger.LogInformation("Getting trace from Tempo: {TraceId}", traceId);

      var response = await _httpClient.GetAsync(url);

      if (!response.IsSuccessStatusCode)
      {
        var error = await response.Content.ReadAsStringAsync();
        return $"Error getting trace: {response.StatusCode} - {error}";
      }

      var content = await response.Content.ReadAsStringAsync();

      // Parse and summarize the trace
      using var doc = JsonDocument.Parse(content);
      var root = doc.RootElement;

      if (root.TryGetProperty("batches", out var batches))
      {
        var spanCount = 0;
        var services = new HashSet<string>();

        foreach (var batch in batches.EnumerateArray())
        {
          if (batch.TryGetProperty("resource", out var resource) &&
              resource.TryGetProperty("attributes", out var attrs))
          {
            foreach (var attr in attrs.EnumerateArray())
            {
              if (attr.TryGetProperty("key", out var key) &&
                  key.GetString() == "service.name" &&
                  attr.TryGetProperty("value", out var val) &&
                  val.TryGetProperty("stringValue", out var svc))
              {
                services.Add(svc.GetString() ?? "unknown");
              }
            }
          }

          if (batch.TryGetProperty("scopeSpans", out var scopeSpans))
          {
            foreach (var scope in scopeSpans.EnumerateArray())
            {
              if (scope.TryGetProperty("spans", out var spans))
              {
                spanCount += spans.GetArrayLength();
              }
            }
          }
        }

        return JsonSerializer.Serialize(new
        {
          TraceId = traceId,
          SpanCount = spanCount,
          Services = services.ToList()
        }, new JsonSerializerOptions { WriteIndented = true });
      }

      return content;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting trace");
      return $"Error getting trace: {ex.Message}";
    }
  }

  private static string NormalizeTraceQLQuery(string query)
  {
    query = query.Trim();

    // Remove braces if present, we'll add them back
    if (query.StartsWith('{'))
      query = query.Substring(1);
    if (query.EndsWith('}'))
      query = query.Substring(0, query.Length - 1);
    query = query.Trim();

    // If it's empty, return a query that matches all traces
    if (string.IsNullOrWhiteSpace(query))
      return "{}";

    // Handle bare HTTP methods ONLY if that's the entire query (e.g., just "DELETE")
    var httpMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
    var upperQuery = query.ToUpperInvariant();
    foreach (var method in httpMethods)
    {
      if (upperQuery == method)
        return $"{{span.http.request.method=\"{method}\"}}";
    }

    // Handle bare "error" or "errors" query
    if (upperQuery is "ERROR" or "ERRORS" or "FAILED" or "FAILURES")
      return "{status=error}";

    // If query doesn't contain an operator, it's likely malformed - try to interpret it
    if (!query.Contains('=') && !query.Contains('>') && !query.Contains('<'))
    {
      // Return empty query to get all traces rather than failing
      return "{}";
    }

    // Map common attribute names to TraceQL equivalents
    query = query.Replace("operation=", "name=");
    query = query.Replace("http.method=", "span.http.request.method=");
    query = System.Text.RegularExpressions.Regex.Replace(query, @"(?<!\.)http\.request\.method=", "span.http.request.method=");

    // Fix unquoted string values in common patterns like http.method=DELETE -> http.method="DELETE"
    // Match pattern: attribute=VALUE where VALUE is unquoted letters
    query = System.Text.RegularExpressions.Regex.Replace(
        query,
        @"([\w.]+)\s*=\s*([A-Za-z_][\w]*)\b(?!"")",
        m =>
        {
          var attr = m.Groups[1].Value;
          var val = m.Groups[2].Value;
          // Don't quote if it's a TraceQL keyword like error, ok, unset
          if (val is "error" or "ok" or "unset" && attr == "status")
            return $"{attr}={val}";
          return $"{attr}=\"{val}\"";
        });

    return "{" + query + "}";
  }

  [KernelFunction]
  [Description("Get a summary of trace activity by HTTP method (GET, POST, PUT, DELETE, etc.) for the specified time range.")]
  public async Task<string> GetTracesSummaryByMethod(
      [Description("Start time in Unix epoch seconds (optional, defaults to last hour)")]
        long? startTime = null,
      [Description("End time in Unix epoch seconds (optional, defaults to now)")]
        long? endTime = null)
  {
    try
    {
      var methods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
      var summary = new Dictionary<string, int>();

      foreach (var method in methods)
      {
        var query = $"{{span.http.request.method=\"{method}\"}}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var start = startTime ?? now - 3600;
        var end = endTime ?? now;

        var url = $"{_tempoBaseUrl}/api/search?q={Uri.EscapeDataString(query)}&limit=1000&start={start}&end={end}";

        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          var result = JsonSerializer.Deserialize<TempoSearchResponse>(content);
          summary[method] = result?.Traces?.Count ?? 0;
        }
        else
        {
          summary[method] = 0;
        }
      }

      var timeRange = $"{DateTimeOffset.FromUnixTimeSeconds(startTime ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600):yyyy-MM-dd HH:mm} to {DateTimeOffset.FromUnixTimeSeconds(endTime ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()):yyyy-MM-dd HH:mm}";

      return JsonSerializer.Serialize(new
      {
        TimeRange = timeRange,
        MethodCounts = summary,
        TotalTraces = summary.Values.Sum()
      }, new JsonSerializerOptions { WriteIndented = true });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting traces summary");
      return $"Error getting traces summary: {ex.Message}";
    }
  }
}

public class TempoSearchResponse
{
  [JsonPropertyName("traces")]
  public List<TempoTrace>? Traces { get; set; }
}

public class TempoTrace
{
  [JsonPropertyName("traceID")]
  public string TraceId { get; set; } = string.Empty;

  [JsonPropertyName("rootServiceName")]
  public string RootServiceName { get; set; } = string.Empty;

  [JsonPropertyName("rootTraceName")]
  public string RootTraceName { get; set; } = string.Empty;

  [JsonPropertyName("startTimeUnixNano")]
  public string StartTimeUnixNano { get; set; } = string.Empty;

  [JsonPropertyName("durationMs")]
  public int DurationMs { get; set; }
}
