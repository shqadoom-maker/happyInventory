using System.Security.Claims;
using Serilog.Context;

namespace HappyInventory.Api.Middleware;

/// <summary>
/// Adds the authenticated user and request trace to the Serilog logical
/// context so service and controller logs can be correlated with the request
/// log entry.
/// </summary>
public class LogContextMiddleware
{
    private readonly RequestDelegate _next;

    public LogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty(
            "UserId",
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)))
        using (LogContext.PushProperty(
            "UserName",
            context.User.FindFirstValue(ClaimTypes.Name)))
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}
