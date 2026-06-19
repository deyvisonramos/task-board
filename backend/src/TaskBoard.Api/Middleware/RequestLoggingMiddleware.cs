using System.Diagnostics;

namespace TaskBoard.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var failed = false;

        try
        {
            await _next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = failed
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;
            var succeeded = !failed && statusCode < StatusCodes.Status400BadRequest;

            _logger.LogInformation(
                "HTTP request completed. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, ElapsedMilliseconds: {ElapsedMilliseconds}, CorrelationId: {CorrelationId}, UserId: {UserId}, Succeeded: {Succeeded}",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                GetCorrelationId(context),
                GetUserId(context),
                succeeded);
        }
    }

    private static string? GetCorrelationId(HttpContext context)
    {
        return context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var value)
            ? value as string
            : null;
    }

    private static string? GetUserId(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out _)
            ? subject
            : null;
    }
}
