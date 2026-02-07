namespace S3Explorer.Models;

public class ActivityLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Message { get; set; } = "";
    public LogLevel Level { get; set; } = LogLevel.Info;
    public double? ProgressPercent { get; set; }
    public bool IsComplete { get; set; }

    public string Display => $"[{Timestamp:HH:mm:ss}] {Message}{(ProgressPercent.HasValue ? $" ({ProgressPercent:0}%)" : "")}";
}

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}
