using TaskBoard.Api.Responses;

namespace TaskBoard.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var correlationId = GetCorrelationId(context);
            _logger.LogError(
                exception,
                "Unhandled exception. CorrelationId: {CorrelationId}",
                correlationId);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Results.Json(
                new ApiErrorResponse(
                    "Unexpected.Error",
                    _environment.IsDevelopment()
                    ? exception.Message
                    : "The request failed unexpectedly.",
                    []),
                statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        return context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var value)
            && value is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId)
                ? correlationId
                : CorrelationIdMiddleware.GetCorrelationId(context);
    }
}
