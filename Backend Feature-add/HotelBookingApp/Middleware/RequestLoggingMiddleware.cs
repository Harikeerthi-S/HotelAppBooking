using System.Diagnostics;
using HotelBookingApp.Delegates;

namespace HotelBookingApp.Middleware
{
    /// <summary>
    /// Logs every HTTP request with method, path, status code and elapsed time.
    /// Uses <see cref="LogFormatterDelegate"/> from AppDelegateFactory.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        // Delegate-based log formatter
        private readonly LogFormatterDelegate _logFormatter =
            AppDelegateFactory.StandardLogFormatter;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw     = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path   = context.Request.Path;

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var statusCode = context.Response.StatusCode;
                var level      = statusCode >= 500 ? "ERROR"
                               : statusCode >= 400 ? "WARN"
                               : "INFO";

                var formatted = _logFormatter(
                    level,
                    "HTTP",
                    $"{method} {path} → {statusCode} ({sw.ElapsedMilliseconds}ms)"
                );

                if (statusCode >= 500)
                    _logger.LogError(formatted);
                else if (statusCode >= 400)
                    _logger.LogWarning(formatted);
                else
                    _logger.LogInformation(formatted);
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
            => app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
