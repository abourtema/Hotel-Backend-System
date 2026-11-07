namespace HotelResortBooking.Api.Middleware;

/// <summary>
/// Skeleton middleware for request/response logging.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        _logger.LogInformation("HTTP {Method} {Path} started", method, path);

        try
        {
            await _next(context);
            _logger.LogInformation(
                "HTTP {Method} {Path} completed with {StatusCode}",
                method,
                path,
                context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP {Method} {Path} failed", method, path);
            throw;
        }
    }
}
