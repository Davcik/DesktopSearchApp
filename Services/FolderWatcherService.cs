using DesktopSearchApp.Models;
using System.IO;

namespace DesktopSearchApp.Services;

public sealed class FolderWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;

    public event Action<FileChangeEvent>? FileChanged;

    public void Start(string folderPath)
    {
        Stop();

        if (!Directory.Exists(folderPath))
            return;

        _watcher = new FileSystemWatcher(folderPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.CreationTime,
            Filter = "*.*",
            EnableRaisingEvents = true
        };

        _watcher.Created += OnCreated;
        _watcher.Changed += OnChanged;
        _watcher.Deleted += OnDeleted;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
    }

    public void Stop()
    {
        if (_watcher == null)
            return;

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnCreated;
        _watcher.Changed -= OnChanged;
        _watcher.Deleted -= OnDeleted;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnError;
        _watcher.Dispose();
        _watcher = null;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(new FileChangeEvent
        {
            ChangeType = FileChangeType.Created,
            FullPath = e.FullPath
        });
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(new FileChangeEvent
        {
            ChangeType = FileChangeType.Changed,
            FullPath = e.FullPath
        });
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        FileChanged?.Invoke(new FileChangeEvent
        {
            ChangeType = FileChangeType.Deleted,
            FullPath = e.FullPath
        });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        FileChanged?.Invoke(new FileChangeEvent
        {
            ChangeType = FileChangeType.Renamed,
            FullPath = e.FullPath,
            OldFullPath = e.OldFullPath
        });
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        // Optional: add logging later
    }

    public void Dispose()
    {
        Stop();
    }
}
