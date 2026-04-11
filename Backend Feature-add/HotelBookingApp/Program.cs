using System.Security.Claims;
using System.Text;
using HotelBookingApp.Context;
using HotelBookingApp.Helpers;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Middleware;
using HotelBookingApp.Repositories;
using HotelBookingApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// ════════════════════════════════════════════════════════════════════════════
//  BUILDER
// ════════════════════════════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Controllers ───────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                statusCode = 400,
                message    = "Validation failed.",
                errors,
                timestamp  = DateTime.UtcNow
            });
        };
    });

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Hotel Booking API",
        Version     = "v1",
        Description = "Hotel Booking WEB API — .NET 10 | EF Core | JWT Auth",
        Contact     = new OpenApiContact { Name = "StayEase Dev Team" }
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste your JWT token here. Example: Bearer eyJhbGci..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Database — SQL Server (Entity Framework Core) ─────────────────────────
var connectionString = builder.Configuration.GetConnectionString("HotelBookingDb")
    ?? throw new InvalidOperationException(
        "Connection string 'HotelBookingDb' not found in appsettings.json.");

builder.Services.AddDbContext<HotelBookingContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    )
);

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

// ── JWT Authentication ────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("JWT Key not found in appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType            = ClaimTypes.Role,
            NameClaimType            = ClaimTypes.Name,
            ClockSkew                = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode  = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 401,
                    message    = "Unauthorized. Please provide a valid JWT token.",
                    timestamp  = DateTime.UtcNow
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode  = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 403,
                    message    = "Forbidden. You do not have permission to access this resource.",
                    timestamp  = DateTime.UtcNow
                });
            }
        };
    });

builder.Services.AddAuthorization();

// ── Generic Repository ────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

// ── Application Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordService,              PasswordHelper>();
builder.Services.AddScoped<IAuthService,                  AuthService>();
builder.Services.AddScoped<IUserService,                  UserService>();
builder.Services.AddScoped<IHotelService,                 HotelService>();
builder.Services.AddScoped<IRoomService,                  RoomService>();
builder.Services.AddScoped<IBookingService,               BookingService>();
builder.Services.AddScoped<IPaymentService,               PaymentService>();
builder.Services.AddScoped<ICancellationService,          CancellationService>();
builder.Services.AddScoped<IReviewService,                ReviewService>();
builder.Services.AddScoped<IAmenityService,               AmenityService>();
builder.Services.AddScoped<IHotelAmenityService,          HotelAmenityService>();
builder.Services.AddScoped<IWishlistService,              WishlistService>();
builder.Services.AddScoped<INotificationService,          NotificationService>();
builder.Services.AddScoped<IAuditLogService,              AuditLogService>();
builder.Services.AddScoped<IChatService,                  ChatService>();
builder.Services.AddScoped<IWalletService,                WalletService>();
builder.Services.AddScoped<JwtTokenHelper>();

// ════════════════════════════════════════════════════════════════════════════
//  BUILD
// ════════════════════════════════════════════════════════════════════════════
var app = builder.Build();


// ── Global Exception Handler (must be first middleware) ───────────────────
app.UseGlobalExceptionHandler();

// ── Request Logger ────────────────────────────────────────────────────────
app.UseRequestLogging();

// ── Swagger ───────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Booking API v1");
    c.RoutePrefix = string.Empty;
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
    c.EnableTryItOutByDefault();
});

// ── CORS ──────────────────────────────────────────────────────────────────
app.UseCors();

// ── Static files — serve uploads folder explicitly ───────────────────────
var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(Path.Combine(uploadsRoot, "reviews"));

// Serve /uploads/** directly from the physical folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath  = "/uploads"
});

// Also serve everything else under wwwroot (fallback)
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (Directory.Exists(wwwrootPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(wwwrootPath),
        RequestPath  = ""
    });
}

// ── Authentication & Authorization ────────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// ── Controllers ───────────────────────────────────────────────────────────
app.MapControllers();

app.Run();

