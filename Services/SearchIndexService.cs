using System.IO;
using System.Windows;
using DesktopSearchApp.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace DesktopSearchApp.Services;

public sealed class SearchIndexService
{
    private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
    private readonly string _indexPath;
    private readonly DocumentExtractionService _documentExtractionService;
    private readonly DiagnosticsLogService _diagnosticsLogService;

    public SearchIndexService(DiagnosticsLogService diagnosticsLogService)
    {
        _indexPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopSearchApp",
            "Index");

        System.IO.Directory.CreateDirectory(_indexPath);

        _diagnosticsLogService = diagnosticsLogService;
        _documentExtractionService = new DocumentExtractionService();
    }

    public async Task BuildIndexAsync(IEnumerable<string> filePaths)
    {
        await Task.Run(() =>
        {
            using var directory = FSDirectory.Open(_indexPath);
            using var analyzer = new StandardAnalyzer(AppLuceneVersion);

            var config = new IndexWriterConfig(AppLuceneVersion, analyzer)
            {
                OpenMode = OpenMode.CREATE
            };

            using var writer = new IndexWriter(directory, config);

            foreach (var filePath in filePaths)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(filePath))
                        continue;

                    if (!File.Exists(filePath))
                    {
                        LogWarning("Skipped file because it does not exist.", filePath);
                        continue;
                    }

                    var extracted = _documentExtractionService.Extract(filePath);
                    var doc = CreateLuceneDocument(extracted);
                    writer.AddDocument(doc);
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogWarning($"Skipped file due to access error: {ex.Message}", filePath);
                    continue;
                }
                catch (IOException ex)
                {
                    LogWarning($"Skipped file due to I/O error: {ex.Message}", filePath);
                    continue;
                }
                catch (IndexOutOfRangeException ex)
                {
                    LogWarning($"Skipped file due to indexing bug: {ex.Message}", filePath);
                    continue;
                }
                catch (Exception ex)
                {
                    LogWarning($"Skipped file due to unexpected error: {ex.Message}", filePath);
                    continue;
                }
            }

            writer.Commit();
        });
    }

    public async Task UpsertFileAsync(string filePath)
    {
        await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                if (!File.Exists(filePath))
                {
                    LogWarning("Skipped file because it does not exist.", filePath);
                    return;
                }

                using var directory = FSDirectory.Open(_indexPath);
                using var analyzer = new StandardAnalyzer(AppLuceneVersion);

                var config = new IndexWriterConfig(AppLuceneVersion, analyzer)
                {
                    OpenMode = OpenMode.CREATE_OR_APPEND
                };

                using var writer = new IndexWriter(directory, config);

                var extracted = _documentExtractionService.Extract(filePath);
                var doc = CreateLuceneDocument(extracted);

                writer.UpdateDocument(new Term("filepath", filePath), doc);
                writer.Commit();
            }
            catch (UnauthorizedAccessException ex)
            {
                LogWarning($"Skipped file due to access error: {ex.Message}", filePath);
            }
            catch (IOException ex)
            {
                LogWarning($"Skipped file due to I/O error: {ex.Message}", filePath);
            }
            catch (IndexOutOfRangeException ex)
            {
                LogWarning($"Skipped file due to indexing bug: {ex.Message}", filePath);
            }
            catch (Exception ex)
            {
                LogWarning($"Skipped file due to unexpected error: {ex.Message}", filePath);
            }
        });
    }

    public async Task DeleteFileAsync(string filePath)
    {
        await Task.Run(() =>
        {
            using var directory = FSDirectory.Open(_indexPath);
            using var analyzer = new StandardAnalyzer(AppLuceneVersion);

            var config = new IndexWriterConfig(AppLuceneVersion, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };

            using var writer = new IndexWriter(directory, config);
            writer.DeleteDocuments(new Term("filepath", filePath));
            writer.Commit();
        });
    }

    public async Task RenameFileAsync(string oldPath, string newPath)
    {
        await DeleteFileAsync(oldPath);

        if (File.Exists(newPath))
            await UpsertFileAsync(newPath);
    }

    public async Task<List<SearchResultItem>> SearchAsync(SearchRequest request)
    {
        return await Task.Run(() =>
        {
            var results = new List<SearchResultItem>();

            using var directory = FSDirectory.Open(_indexPath);

            if (!DirectoryReader.IndexExists(directory))
                return results;

            using var reader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(reader);
            using var analyzer = new StandardAnalyzer(AppLuceneVersion);

            if (string.IsNullOrWhiteSpace(request.QueryText))
                return results;

            Query textQuery = BuildCombinedSearchQuery(request, analyzer);
            Query finalQuery = textQuery;

            if (request.AllowedExtensions != null && request.AllowedExtensions.Any())
            {
                var extensionQuery = new BooleanQuery();

                foreach (var ext in request.AllowedExtensions)
                {
                    extensionQuery.Add(
                        new TermQuery(new Term("extension", ext.ToLowerInvariant())),
                        Occur.SHOULD);
                }

                finalQuery = new BooleanQuery
                {
                    { textQuery, Occur.MUST },
                    { extensionQuery, Occur.MUST }
                };
            }

            var topDocs = searcher.Search(finalQuery, 200);

            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);

                string title = doc.Get("title") ?? "";
                string fileName = doc.Get("filename") ?? "";
                string extension = doc.Get("extension") ?? "";
                string filePath = doc.Get("filepath") ?? "";
                string documentText = doc.Get("documenttext") ?? "";

                bool ocrUsed = string.Equals(doc.Get("ocrused"), "true", StringComparison.OrdinalIgnoreCase);
                string extractionMethod = doc.Get("extractionmethod") ?? "";
                string extractionStatus = doc.Get("extractionstatus") ?? "";
                string errorMessage = doc.Get("errormessage") ?? "";

                int extractedTextLength = 0;
                int.TryParse(doc.Get("extractedtextlength"), out extractedTextLength);

                DateTime? lastModified = null;
                if (File.Exists(filePath))
                    lastModified = File.GetLastWriteTime(filePath);

                results.Add(new SearchResultItem
                {
                    FilePath = filePath,
                    FileName = fileName,
                    Extension = extension,
                    Title = title,
                    DocumentText = documentText,
                    Snippet = BuildSnippet(documentText, request.QueryText),
                    OcrUsed = ocrUsed,
                    ExtractionMethod = extractionMethod,
                    ExtractedTextLength = extractedTextLength,
                    ExtractionStatus = extractionStatus,
                    ErrorMessage = errorMessage,
                    LastModified = lastModified
                });
            }

            return results;
        });
    }

    private void LogWarning(string message, string filePath)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher == null)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            _diagnosticsLogService.Warning(message, filePath);
        }
        else
        {
            dispatcher.Invoke(() => _diagnosticsLogService.Warning(message, filePath));
        }
    }

    private Query BuildCombinedSearchQuery(SearchRequest request, StandardAnalyzer analyzer)
    {
        string exactQueryText = BuildExactQueryText(request.QueryText);
        string prefixQueryText = BuildPrefixQueryText(request.QueryText);

        Query exactQuery = BuildParsedQuery(request.Scope, analyzer, exactQueryText);
        Query prefixQuery = BuildParsedQuery(request.Scope, analyzer, prefixQueryText);

        exactQuery.Boost = 3.0f;
        prefixQuery.Boost = 1.5f;

        var combined = new BooleanQuery
        {
            { exactQuery, Occur.SHOULD },
            { prefixQuery, Occur.SHOULD }
        };

        return combined;
    }

    private Query BuildParsedQuery(SearchScope scope, StandardAnalyzer analyzer, string queryText)
    {
        if (scope == SearchScope.TitleOnly)
        {
            var parser = new QueryParser(AppLuceneVersion, "title", analyzer);
            return parser.Parse(queryText);
        }

        if (scope == SearchScope.DocumentText)
        {
            var parser = new QueryParser(AppLuceneVersion, "documenttext", analyzer);
            return parser.Parse(queryText);
        }

        var multiFieldParser = new MultiFieldQueryParser(
            AppLuceneVersion,
            new[] { "title", "documenttext", "filename" },
            analyzer);

        return multiFieldParser.Parse(queryText);
    }

    private static string BuildExactQueryText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var terms = input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(QueryParser.Escape);

        return string.Join(" ", terms);
    }

    private static string BuildPrefixQueryText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var terms = input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(term =>
            {
                string escaped = QueryParser.Escape(term);

                if (escaped.Length < 2)
                    return escaped;

                if (escaped.EndsWith("*") || escaped.EndsWith("?"))
                    return escaped;

                return escaped + "*";
            });

        return string.Join(" ", terms);
    }

    private Document CreateLuceneDocument(IndexedDocument extracted)
    {
        return new Document
        {
            new StringField("filepath", extracted.FilePath, Field.Store.YES),
            new StringField("filename", extracted.FileName, Field.Store.YES),
            new StringField("extension", extracted.Extension, Field.Store.YES),
            new StringField("ocrused", extracted.OcrUsed ? "true" : "false", Field.Store.YES),

            new StoredField("extractionmethod", extracted.Diagnostics.ExtractionMethod ?? ""),
            new StoredField("extractionstatus", extracted.Diagnostics.Status ?? ""),
            new StoredField("errormessage", extracted.Diagnostics.ErrorMessage ?? ""),
            new StoredField("extractedtextlength", extracted.Diagnostics.ExtractedTextLength),

            new TextField("title", extracted.Title ?? "", Field.Store.YES),
            new TextField("documenttext", extracted.DocumentText ?? "", Field.Store.YES)
        };
    }

    private static string BuildSnippet(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "No text snippet available.";

        int index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
            return text.Length > 220 ? text[..220] + "..." : text;

        int start = Math.Max(0, index - 70);
        int length = Math.Min(220, text.Length - start);
        return text.Substring(start, length).Replace(Environment.NewLine, " ");
    }
}