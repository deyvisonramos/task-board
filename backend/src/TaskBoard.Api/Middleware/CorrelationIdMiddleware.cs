namespace TaskBoard.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "CorrelationId";
    private const int MaxCorrelationIdLength = 128;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = GetCorrelationId(context);
        context.Items[ItemName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            [ItemName] = correlationId
        }))
        {
            await _next(context);
        }
    }

    public static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var suppliedCorrelationId = values.FirstOrDefault();

            if (IsValidCorrelationId(suppliedCorrelationId))
            {
                return suppliedCorrelationId!;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidCorrelationId(string? correlationId)
    {
        return !string.IsNullOrWhiteSpace(correlationId)
            && correlationId.Length <= MaxCorrelationIdLength
            && correlationId.All(character => character is >= '!' and <= '~');
    }
}
