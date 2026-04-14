using System.Diagnostics;

namespace PatientTracker.API.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        
        _logger.LogInformation("HTTP {Method} {Path} started", request.Method, request.Path);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP {Method} {Path} failed", request.Method, request.Path);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var response = context.Response;
            _logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms", 
                request.Method, request.Path, response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
