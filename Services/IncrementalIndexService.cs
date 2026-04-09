using DesktopSearchApp.Models;
using System.Collections.Concurrent;
using System.IO;

namespace DesktopSearchApp.Services;

public sealed class IncrementalIndexService : IDisposable
{
    private readonly SearchIndexService _searchIndexService;
    private readonly FileCrawlerService _fileCrawlerService;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingChanges = new();

    public IncrementalIndexService(SearchIndexService searchIndexService, FileCrawlerService fileCrawlerService)
    {
        _searchIndexService = searchIndexService;
        _fileCrawlerService = fileCrawlerService;
    }

    public void QueueChange(FileChangeEvent changeEvent)
    {
        string key = changeEvent.FullPath;

        if (_pendingChanges.TryRemove(key, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _pendingChanges[key] = cts;

        _ = ProcessChangeAsync(changeEvent, cts.Token);
    }

    private async Task ProcessChangeAsync(FileChangeEvent changeEvent, CancellationToken token)
    {
        try
        {
            await Task.Delay(900, token);

            if (token.IsCancellationRequested)
                return;

            var ext = Path.GetExtension(changeEvent.FullPath).ToLowerInvariant();

            bool supported =
                _fileCrawlerService.IsSupportedExtension(ext) ||
                (changeEvent.ChangeType == FileChangeType.Renamed &&
                 changeEvent.OldFullPath is not null &&
                 _fileCrawlerService.IsSupportedExtension(Path.GetExtension(changeEvent.OldFullPath).ToLowerInvariant()));

            if (!supported)
                return;

            switch (changeEvent.ChangeType)
            {
                case FileChangeType.Created:
                case FileChangeType.Changed:
                    if (File.Exists(changeEvent.FullPath))
                        await _searchIndexService.UpsertFileAsync(changeEvent.FullPath);
                    break;

                case FileChangeType.Deleted:
                    await _searchIndexService.DeleteFileAsync(changeEvent.FullPath);
                    break;

                case FileChangeType.Renamed:
                    if (!string.IsNullOrWhiteSpace(changeEvent.OldFullPath))
                        await _searchIndexService.RenameFileAsync(changeEvent.OldFullPath, changeEvent.FullPath);
                    break;
            }
        }
        catch (TaskCanceledException)
        {
        }
        finally
        {
            if (_pendingChanges.TryRemove(changeEvent.FullPath, out var cts))
            {
                cts.Dispose();
            }
        }
    }

    public void Dispose()
    {
        foreach (var item in _pendingChanges.Values)
        {
            item.Cancel();
            item.Dispose();
        }

        _pendingChanges.Clear();
    }
}