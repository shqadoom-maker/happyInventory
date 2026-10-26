namespace HappyInventory.Core.Entities;

/// <summary>
/// A structured application log entry persisted by the Serilog database sink.
/// Entries are read-only from the application's point of view; only the
/// background log writer inserts them and administrators may prune old rows.
/// </summary>
public class SystemLog
{
    public long Id { get; set; }

    /// <summary>UTC timestamp of the log event.</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Serilog level name: Verbose, Debug, Information, Warning, Error or Fatal.</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>The rendered log message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The original structured message template, when available.</summary>
    public string? MessageTemplate { get; set; }

    /// <summary>Full exception text (type, message and stack trace), when available.</summary>
    public string? Exception { get; set; }

    /// <summary>The logger/source context that emitted the event.</summary>
    public string? SourceContext { get; set; }

    /// <summary>The authenticated user's identifier, when the event happened in a request.</summary>
    public string? UserId { get; set; }

    /// <summary>The authenticated user's username, when the event happened in a request.</summary>
    public string? UserName { get; set; }

    /// <summary>The request path, when the event happened in an HTTP request.</summary>
    public string? RequestPath { get; set; }

    /// <summary>The HTTP method, when the event happened in an HTTP request.</summary>
    public string? RequestMethod { get; set; }

    /// <summary>The HTTP response status code, when available.</summary>
    public int? StatusCode { get; set; }

    /// <summary>The ASP.NET Core request trace identifier.</summary>
    public string? TraceId { get; set; }

    /// <summary>Additional structured properties serialized as JSON.</summary>
    public string? Properties { get; set; }
}
