using Microsoft.AspNetCore.Http;

namespace HappyInventory.Api.Middleware;

/// <summary>
/// Blocks authenticated access to all endpoints except the password change
/// endpoint when the signed-in user must change their password at first login.
/// </summary>
public class RequirePasswordChangeMiddleware
{
    private const string ChangePasswordPath = "/api/auth/change-password";

    private readonly RequestDelegate _next;

    public RequirePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            var mustChange = context.User.FindFirst("mustChangePassword")?.Value;

            if (bool.TryParse(mustChange, out var required) && required)
            {
                var path = context.Request.Path.Value ?? string.Empty;

                // Allow the password change endpoint and the OpenAPI/Swagger
                // UI so the developer can still test the API during development.
                if (!string.Equals(path, ChangePasswordPath, StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(
                        "{\"title\":\"Password change required\",\"detail\":\"You must change your password before accessing other endpoints.\"}");

                    return;
                }
            }
        }

        await _next(context);
    }
}
