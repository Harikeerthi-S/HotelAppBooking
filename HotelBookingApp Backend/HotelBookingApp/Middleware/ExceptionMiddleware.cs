using System.Net;
using System.Text.Json;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Middleware
{
    /// <summary>
    /// Global exception middleware — catches all unhandled exceptions and
    /// maps them to structured JSON error responses.
    /// Handles: App exceptions, EF Core errors, cancellation, and generic 500s.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate             _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment            _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next   = next;
            _logger = logger;
            _env    = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Don't log cancellation as errors — client disconnected
                if (ex is OperationCanceledException or TaskCanceledException)
                {
                    _logger.LogWarning("Request cancelled: {Method} {Path}",
                        context.Request.Method, context.Request.Path);
                    if (!context.Response.HasStarted)
                        context.Response.StatusCode = 499; // Client Closed Request
                    return;
                }

                _logger.LogError(ex,
                    "Unhandled exception on {Method} {Path}: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started — cannot write error response.");
                return;
            }

            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                // ── App-defined exceptions ────────────────────────────────
                BadRequestException    e => (HttpStatusCode.BadRequest,          e.Message),
                UnauthorizedException  e => (HttpStatusCode.Unauthorized,        e.Message),
                ForbiddenException     e => (HttpStatusCode.Forbidden,           e.Message),
                NotFoundException      e => (HttpStatusCode.NotFound,            e.Message),
                AlreadyExistsException e => (HttpStatusCode.Conflict,            e.Message),
                ValidationException    e => (HttpStatusCode.UnprocessableEntity, e.Message),

                // ── File / IO exceptions ──────────────────────────────────
                DirectoryNotFoundException =>
                    (HttpStatusCode.InternalServerError,
                     "A required server directory is missing. Please contact the administrator."),
                FileNotFoundException =>
                    (HttpStatusCode.NotFound,
                     "A required file was not found on the server."),
                UnauthorizedAccessException =>
                    (HttpStatusCode.Forbidden,
                     "Access to a server resource was denied."),

                // ── EF Core / DB exceptions ───────────────────────────────
                DbUpdateConcurrencyException =>
                    (HttpStatusCode.Conflict,
                     "The record was modified by another user. Please refresh and try again."),

                DbUpdateException dbEx =>
                    (HttpStatusCode.BadRequest,
                     ExtractDbMessage(dbEx)),

                // ── Generic fallback ──────────────────────────────────────
                _ => (HttpStatusCode.InternalServerError,
                      "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponseDto
            {
                StatusCode = (int)statusCode,
                Message    = message,
                Details    = _env.IsDevelopment() ? exception.ToString() : null,
                Timestamp  = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        /// <summary>Extracts a user-friendly message from EF Core DbUpdateException.</summary>
        private static string ExtractDbMessage(DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;

            // SQL Server constraint violations
            if (inner.Contains("UNIQUE") || inner.Contains("duplicate key"))
                return "A record with this value already exists.";
            if (inner.Contains("FOREIGN KEY") || inner.Contains("REFERENCE"))
                return "This operation violates a data relationship constraint.";
            if (inner.Contains("Cannot insert the value NULL"))
                return "A required field is missing.";
            if (inner.Contains("String or binary data would be truncated"))
                return "One or more values exceed the maximum allowed length.";

            return "A database error occurred. Please check your input and try again.";
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionMiddleware>();
    }
}
