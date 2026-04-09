namespace DesktopSearchApp.Models;

public enum FileChangeType
{
    Created = 0,
    Changed = 1,
    Deleted = 2,
    Renamed = 3
}

public sealed class FileChangeEvent
{
    public FileChangeType ChangeType { get; set; }
    public string FullPath { get; set; } = "";
    public string? OldFullPath { get; set; }
}
