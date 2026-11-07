using System.Text.Json;
using Serilog.Core;
using Serilog.Events;

namespace Bariza.Api.Logging;

/// <summary>
/// Serilog sink that forwards structured events to the database writer. It
/// never performs I/O on the request thread, never throws, and redacts common
/// secret-bearing property names before the event is stored.
/// </summary>
public sealed class SystemLogSink : ILogEventSink
{
    private const int MaxMessageLength = 4000;
    private const int MaxTemplateLength = 4000;
    private const int MaxExceptionLength = 8000;
    private const int MaxSourceContextLength = 256;
    private const int MaxUserIdLength = 64;
    private const int MaxUserNameLength = 120;
    private const int MaxPathLength = 512;
    private const int MaxMethodLength = 16;
    private const int MaxTraceIdLength = 64;
    private const int MaxPropertiesLength = 8000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> MappedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "SourceContext",
        "UserId",
        "UserName",
        "RequestPath",
        "RequestMethod",
        "StatusCode",
        "TraceId",
    };

    private static readonly string[] SensitivePropertyTokens =
    {
        "password",
        "secret",
        "token",
        "authorization",
        "api-key",
        "apikey",
        "api_key",
        "cookie",
        "credential",
        "jwt",
    };

    private readonly SystemLogQueue _queue;
    private readonly LogEventLevel _minimumLevel;

    public SystemLogSink(SystemLogQueue queue, LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        _queue = queue;
        _minimumLevel = minimumLevel;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < _minimumLevel)
        {
            return;
        }

        try
        {
            var entry = new SystemLogEntry
            {
                TimestampUtc = logEvent.Timestamp.UtcDateTime,
                Level = logEvent.Level.ToString(),
                Message = Truncate(RenderMessage(logEvent), MaxMessageLength) ?? string.Empty,
                MessageTemplate = Truncate(logEvent.MessageTemplate.Text, MaxTemplateLength),
                Exception = Truncate(logEvent.Exception?.ToString(), MaxExceptionLength),
                SourceContext = Truncate(GetScalar(logEvent, "SourceContext"), MaxSourceContextLength),
                UserId = Truncate(GetScalar(logEvent, "UserId"), MaxUserIdLength),
                UserName = Truncate(GetScalar(logEvent, "UserName"), MaxUserNameLength),
                RequestPath = Truncate(GetScalar(logEvent, "RequestPath"), MaxPathLength),
                RequestMethod = Truncate(GetScalar(logEvent, "RequestMethod"), MaxMethodLength),
                StatusCode = GetInt32(logEvent, "StatusCode"),
                TraceId = Truncate(GetScalar(logEvent, "TraceId"), MaxTraceIdLength),
                Properties = Truncate(SerializeProperties(logEvent), MaxPropertiesLength),
            };

            _queue.TryEnqueue(entry);
        }
        catch
        {
            // Logging must never take down the request or the process. If an
            // event cannot be shaped for storage it is simply skipped.
        }
    }

    private static string? RenderMessage(LogEvent logEvent)
    {
        try
        {
            return logEvent.RenderMessage();
        }
        catch
        {
            return logEvent.MessageTemplate.Text;
        }
    }

    private static string? GetScalar(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
        {
            return null;
        }

        return value switch
        {
            ScalarValue { Value: null } => null,
            ScalarValue scalar => scalar.Value?.ToString(),
            _ => value.ToString(),
        };
    }

    private static int? GetInt32(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
        {
            return null;
        }

        return value switch
        {
            ScalarValue { Value: int number } => number,
            ScalarValue { Value: long number } when number is >= int.MinValue and <= int.MaxValue
                => (int)number,
            ScalarValue { Value: string text } when int.TryParse(text, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string? SerializeProperties(LogEvent logEvent)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (name, value) in logEvent.Properties)
        {
            if (MappedProperties.Contains(name))
            {
                continue;
            }

            if (IsSensitive(name))
            {
                properties[name] = "[REDACTED]";
                continue;
            }

            properties[name] = value switch
            {
                ScalarValue scalar => scalar.Value,
                _ => Truncate(value.ToString(), 1000),
            };
        }

        if (properties.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(properties, JsonOptions);
        }
        catch
        {
            // Fall back to a readable representation for unusual property types.
            return string.Join(
                ", ",
                properties.Select(pair => $"{pair.Key}={pair.Value}"));
        }
    }

    private static bool IsSensitive(string propertyName)
    {
        return SensitivePropertyTokens.Any(
            token => propertyName.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        if (maxLength <= 3)
        {
            return value.Substring(0, maxLength);
        }

        return value.Substring(0, maxLength - 3) + "...";
    }
}
