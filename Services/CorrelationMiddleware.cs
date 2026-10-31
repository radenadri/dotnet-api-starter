namespace bookism.Services;

/// <summary>
/// Intercepts inbound HTTP requests to establish or propagate distributed tracing identifiers,
/// ensuring every request has a unique correlation ID across the entire execution lifecycle.
/// </summary>
public class CorrelationMiddleware
{
    public const string HeaderKey = "X-Request-ID";
    public const string ItemKey = "TraceId";

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string traceId;
        if (context.Request.Headers.TryGetValue(HeaderKey, out var incomingId) && !string.IsNullOrWhiteSpace(incomingId))
        {
            traceId = incomingId.ToString();
        }
        else
        {
            traceId = Guid.NewGuid().ToString();
        }

        context.Items[ItemKey] = traceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderKey] = traceId;
            return Task.CompletedTask;
        });

        await _next(context);
    }

    /// <summary>
    /// Helper to resolve the active request trace ID for error formatting and telemetry.
    /// </summary>
    public static string GetTraceId(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var traceId) && traceId is string id)
        {
            return id;
        }

        return context.TraceIdentifier;
    }
}

