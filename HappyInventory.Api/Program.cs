using System.Security.Claims;
using System.Text;
using HappyInventory.Api.Logging;
using HappyInventory.Api.Middleware;
using HappyInventory.Application;
using HappyInventory.Application.Common;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Core.Enums;
using HappyInventory.Infrastructure;
using HappyInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreRateLimit;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// User Secrets for local development (never commit secrets to source control).
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Configure Serilog structured logging. Database logging is enabled by default
// and is written asynchronously by SystemLogWriterService.
var databaseLoggingEnabled = builder.Configuration.GetValue("Logging:Database:Enabled", true);
SystemLogQueue? systemLogQueue = null;

if (databaseLoggingEnabled)
{
    systemLogQueue = new SystemLogQueue(
        builder.Configuration.GetValue("Logging:Database:QueueCapacity", 10_000));
}

var databaseMinimumLevel = ParseLogLevel(
    builder.Configuration["Logging:Database:MinimumLevel"],
    LogEventLevel.Information);

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception:lj}")
    .WriteTo.File(
        path: "logs/happy-inventory-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception:lj}");

if (systemLogQueue is not null)
{
    loggerConfiguration.WriteTo.Sink(new SystemLogSink(systemLogQueue, databaseMinimumLevel));
}

Log.Logger = loggerConfiguration.CreateLogger();

// Use Serilog as the logging provider.
builder.Host.UseSerilog();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("Jwt:Key is required. Set it via User Secrets (development) or environment variables (production).");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(new AdminPolicyOptions
{
    DefaultAdminUsername = builder.Configuration["Seed:AdminUsername"]
        ?? throw new InvalidOperationException("Seed:AdminUsername is required."),
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

if (systemLogQueue is not null)
{
    builder.Services.AddSingleton(systemLogQueue);
    builder.Services.AddHostedService<SystemLogWriterService>();
    builder.Services.AddHostedService<SystemLogRetentionService>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };

        options.Events = new JwtBearerEvents
        {
            // Reject tokens for deactivated/deleted users and keep the role claim
            // in sync when an administrator changes it.
            OnTokenValidated = async context =>
            {
                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(idValue, out var userId))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                var user = await users.GetAuthStateAsync(
                    userId,
                    context.HttpContext.RequestAborted);

                if (user is null || !user.IsActive)
                {
                    context.Fail("The user is inactive or no longer exists.");
                    return;
                }

                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    var roleClaim = identity.FindFirst(ClaimTypes.Role);

                    if (roleClaim is null || !string.Equals(roleClaim.Value, user.Role, StringComparison.Ordinal))
                    {
                        if (roleClaim is not null)
                        {
                            identity.RemoveClaim(roleClaim);
                        }

                        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
                    }
                }
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Every endpoint requires a valid JWT unless it is explicitly [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.Admin));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Rate limiting to protect sensitive endpoints (especially login) from brute-force attacks.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.ClientIdHeader = "X-Client-Id";
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period = "1m",
            Limit = 5, // 5 login attempts per minute per IP
        },
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 120, // 120 requests per minute per IP for all other endpoints
        },
    };
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Happy Inventory API",
        Version = "v1",
        Description = "Warehouse inventory API. Sign in with POST /api/auth/login, then use the returned JWT. " +
                      "External eCommerce systems post inventory deductions to POST /api/inventory/updates.",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by POST /api/auth/login.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod:l} {RequestPath:l} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.GetLevel = (httpContext, _, exception) =>
        exception is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set(
            "UserId",
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
        diagnosticContext.Set(
            "UserName",
            httpContext.User.FindFirstValue(ClaimTypes.Name));
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serve the Angular production build from wwwroot when the API and client are
// deployed as a single App Service. In development the Angular dev server is
// used instead and wwwroot may not exist.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AngularDev");

// Azure App Service terminates TLS and forwards HTTP to the container. The
// platform's "HTTPS Only" setting handles the public redirect, so only enable
// the local development redirect here.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseIpRateLimiting();
app.UseAuthentication();

// Enrich service/controller logs with the signed-in user and trace id.
app.UseMiddleware<LogContextMiddleware>();

// Enforce first-login password change before authorizing access to endpoints.
app.UseMiddleware<RequirePasswordChangeMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Angular client-side routes fall back to index.html. Keep API routes
// untouched so an unknown /api path still returns 404 instead of the SPA.
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var webRootPath = app.Environment.WebRootPath;

    if (string.IsNullOrWhiteSpace(webRootPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexPath = Path.Combine(webRootPath, "index.html");

    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(indexPath);
}).AllowAnonymous();

await app.RunAsync();

static LogEventLevel ParseLogLevel(string? value, LogEventLevel fallback)
{
    return Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level)
        ? level
        : fallback;
}
