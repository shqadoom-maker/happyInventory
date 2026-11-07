using System.Security.Claims;
using System.Text.Json;
using Bariza.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace Bariza.Api.Middleware;

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
        var (status, title, code) = exception switch
        {
            NotFoundException e => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                e.Code),
            ValidationException e => (
                StatusCodes.Status400BadRequest,
                "Request could not be processed",
                e.Code),
            ConflictException e => (
                StatusCodes.Status409Conflict,
                "Request conflicts with the current state",
                e.Code),
            AuthenticationException e => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                e.Code),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                (string?)null),
        };

        var detail = exception is AppException appException
            ? appException.Message
            : "An unexpected error occurred. Please try again.";

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
                // For expected client errors (4xx), log the domain reason. The
                // reason is written to server logs only and never returned
                // outside the ProblemDetails body.
                _logger.LogInformation(
                    "Request to {Path} {Method} failed with status {Status}: {Reason}",
                    context.Request.Path,
                    context.Request.Method,
                    status,
                    detail);
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

        if (!string.IsNullOrWhiteSpace(code))
        {
            problem.Extensions["code"] = code;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
