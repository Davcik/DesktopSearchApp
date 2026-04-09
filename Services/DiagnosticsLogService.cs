using DesktopSearchApp.Models;
using System.Collections.ObjectModel;

namespace DesktopSearchApp.Services;

public sealed class DiagnosticsLogService
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Info(string message, string filePath = "")
    {
        Add("Info", message, filePath);
    }

    public void Warning(string message, string filePath = "")
    {
        Add("Warning", message, filePath);
    }

    public void Error(string message, string filePath = "")
    {
        Add("Error", message, filePath);
    }

    private void Add(string level, string message, string filePath)
    {
        Entries.Insert(0, new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            FilePath = filePath
        });

        while (Entries.Count > 200)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }
}
