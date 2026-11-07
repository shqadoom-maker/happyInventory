namespace Bariza.Application.DTOs.SystemLogs;

public class SystemLogDto
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MessageTemplate { get; set; }
    public string? Exception { get; set; }
    public string? SourceContext { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? TraceId { get; set; }
    public string? Properties { get; set; }
}
