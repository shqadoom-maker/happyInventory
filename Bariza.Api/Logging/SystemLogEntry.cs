namespace Bariza.Api.Logging;

/// <summary>
/// A lightweight, detached representation of a Serilog event. The Serilog sink
/// fills it on the request thread and the background writer persists it later.
/// </summary>
public sealed class SystemLogEntry
{
    public DateTime TimestampUtc { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? MessageTemplate { get; init; }
    public string? Exception { get; init; }
    public string? SourceContext { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? RequestPath { get; init; }
    public string? RequestMethod { get; init; }
    public int? StatusCode { get; init; }
    public string? TraceId { get; init; }
    public string? Properties { get; init; }
}
