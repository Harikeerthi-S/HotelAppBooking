using System.Net;
using System.Text.Json;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Middleware
{
    /// <summary>
    /// Global exception middleware — catches all unhandled exceptions and
    /// maps them to structured JSON error responses (400 / 401 / 403 / 404 / 409 / 422 / 500).
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

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
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                BadRequestException     e => (HttpStatusCode.BadRequest,          e.Message),
                UnauthorizedException   e => (HttpStatusCode.Unauthorized,        e.Message),
                ForbiddenException      e => (HttpStatusCode.Forbidden,           e.Message),
                NotFoundException       e => (HttpStatusCode.NotFound,            e.Message),
                AlreadyExistsException  e => (HttpStatusCode.Conflict,            e.Message),
                ValidationException     e => (HttpStatusCode.UnprocessableEntity, e.Message),
                _                         => (HttpStatusCode.InternalServerError,
                                              "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponseDto
            {
                StatusCode = (int)statusCode,
                Message    = message,
                Details    = _env.IsDevelopment() ? exception.StackTrace : null,
                Timestamp  = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    // Extension method for clean registration in Program.cs
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionMiddleware>();
    }
}
