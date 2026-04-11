namespace HotelBookingApp.Exceptions
{
    // ── Base application exception ────────────────────────────────────────
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }
        protected AppException(string message, int statusCode = 500)
            : base(message) => StatusCode = statusCode;
        protected AppException(string message, Exception inner, int statusCode = 500)
            : base(message, inner) => StatusCode = statusCode;
    }

    // ── 400 Bad Request ───────────────────────────────────────────────────
    public class BadRequestException : AppException
    {
        public BadRequestException(string message)
            : base(message, 400) { }
        public BadRequestException(string message, Exception inner)
            : base(message, inner, 400) { }
    }

    // ── 404 Not Found ─────────────────────────────────────────────────────
    public class NotFoundException : AppException
    {
        public NotFoundException(string entity, object key)
            : base($"{entity} with id '{key}' was not found.", 404) { }
        public NotFoundException(string message)
            : base(message, 404) { }
    }

    // ── 409 Conflict (already exists / duplicate) ─────────────────────────
    public class AlreadyExistsException : AppException
    {
        public AlreadyExistsException(string message)
            : base(message, 409) { }
    }

    // ── 401 Unauthorized ──────────────────────────────────────────────────
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Unauthorized access.")
            : base(message, 401) { }
    }

    // ── 403 Forbidden ─────────────────────────────────────────────────────
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.")
            : base(message, 403) { }
    }

    // ── 422 Unprocessable Entity ──────────────────────────────────────────
    public class ValidationException : AppException
    {
        public ValidationException(string message)
            : base(message, 422) { }
    }
}
