using System.Security.Claims;
using System.Text;
using HappyInventory.Api.Middleware;
using HappyInventory.Application;
using HappyInventory.Application.Common;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Core.Enums;
using HappyInventory.Infrastructure;
using HappyInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(new AdminPolicyOptions
{
    DefaultAdminUsername = builder.Configuration["Seed:AdminUsername"]
        ?? "admin@happywarehouse.com",
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AngularDev");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
