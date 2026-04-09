namespace DesktopSearchApp.Models;

public sealed class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string FilePath { get; set; } = "";
}
