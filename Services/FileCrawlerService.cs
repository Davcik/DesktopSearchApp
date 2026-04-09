using System.Collections.Generic;
using System.IO;

namespace DesktopSearchApp.Services;

public sealed class FileCrawlerService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".docx", ".pdf", ".txt", ".md", ".jpg", ".jpeg", ".png",
            ".csv", ".xlsx", ".doc", ".dta", ".tex", ".cls", ".bib",
            ".sav", ".py", ".html", ".epub", ".do", ".r", ".rda",
            ".ipynb", ".ado", ".sps", ".sas", ".sas7bdat", ".m", ".mat", ".pptx", ".ppt"
        };

    public IEnumerable<string> GetSupportedFiles(string rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            yield break;

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false
        };

        var folders = new Queue<string>();
        folders.Enqueue(rootFolder);

        while (folders.Count > 0)
        {
            var currentFolder = folders.Dequeue();

            IEnumerable<string> subFolders;
            try
            {
                subFolders = Directory.EnumerateDirectories(currentFolder, "*", options);
            }
            catch
            {
                continue;
            }

            foreach (var subFolder in subFolders)
                folders.Enqueue(subFolder);

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(currentFolder, "*.*", options);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);

                if (IsSupportedExtension(extension))
                    yield return file;
            }
        }
    }

    public bool IsSupportedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return SupportedExtensions.Contains(extension);
    }
}