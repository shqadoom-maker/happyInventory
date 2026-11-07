namespace Bariza.Application.DTOs.SystemLogs;

public class SystemLogCleanupResult
{
    public int DeletedCount { get; set; }
    public int OlderThanDays { get; set; }
    public DateTime CutoffUtc { get; set; }
}
