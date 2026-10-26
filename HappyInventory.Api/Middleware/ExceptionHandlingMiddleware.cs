using System.Security.Claims;
using System.Text.Json;
using HappyInventory.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace HappyInventory.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message),
            ConflictException => (
                StatusCodes.Status409Conflict,
                "Request conflicts with the current state",
                exception.Message),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Request could not be processed",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred. Please try again."),
        };

        using (LogContext.PushProperty(
            "UserId",
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)))
        using (LogContext.PushProperty(
            "UserName",
            context.User.FindFirstValue(ClaimTypes.Name)))
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        {
            if (status >= StatusCodes.Status500InternalServerError)
            {
                // Log the full exception for server errors (diagnostics only),
                // but never return exception details to the client.
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing {Path} {Method}",
                    context.Request.Path,
                    context.Request.Method);
            }
            else
            {
                // For client errors (4xx), log a safe message without the raw
                // exception detail to avoid leaking sensitive information.
                _logger.LogInformation(
                    "Request to {Path} {Method} failed with status {Status}",
                    context.Request.Path,
                    context.Request.Method,
                    status);
            }
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
