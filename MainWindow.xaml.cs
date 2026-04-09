using DesktopSearchApp.Models;
using DesktopSearchApp.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Input;
using System.Diagnostics;

namespace DesktopSearchApp;

public partial class MainWindow : Window
{
    private readonly SearchIndexService _searchIndexService;
    private readonly FileCrawlerService _fileCrawlerService;
    private readonly PreviewService _previewService;
    private readonly FolderWatcherService _folderWatcherService;
    private readonly IncrementalIndexService _incrementalIndexService;
    private readonly DiagnosticsLogService _diagnosticsLogService;

    private string? _selectedFolder;

    public ObservableCollection<LogEntry> DiagnosticsEntries => _diagnosticsLogService.Entries;

    public MainWindow()
    {
        InitializeComponent();

        _diagnosticsLogService = new DiagnosticsLogService();
        _searchIndexService = new SearchIndexService(_diagnosticsLogService);
        _fileCrawlerService = new FileCrawlerService();
        _previewService = new PreviewService();
        _folderWatcherService = new FolderWatcherService();
        _incrementalIndexService = new IncrementalIndexService(_searchIndexService, _fileCrawlerService);
        _diagnosticsLogService = new DiagnosticsLogService();

        DataContext = this;

        _folderWatcherService.FileChanged += OnFileChanged;

        _diagnosticsLogService.Info("Application started.");
        StatusText.Text = "Status: ready";

        var lastFolder = Properties.Settings.Default.LastIndexedFolder;

        if (!string.IsNullOrWhiteSpace(lastFolder) && Directory.Exists(lastFolder))
        {
            _selectedFolder = lastFolder;
            SelectedFolderText.Text = _selectedFolder;
            StatusText.Text = "Status: restored last indexed folder";

            _folderWatcherService.Start(_selectedFolder);
            _diagnosticsLogService.Info("Restored last indexed folder.", _selectedFolder);
        }
    }

    private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a folder to index"
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedFolder = dialog.FolderName;
            SelectedFolderText.Text = _selectedFolder;
            StatusText.Text = "Status: folder selected";

            Properties.Settings.Default.LastIndexedFolder = _selectedFolder;
            Properties.Settings.Default.Save();

            _diagnosticsLogService.Info("Folder selected for indexing.", _selectedFolder);
        }
        else
        {
            _diagnosticsLogService.Info("Folder selection cancelled.");
        }
    }

    private async void BuildIndexButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFolder))
        {
            MessageBox.Show("Please select a folder first.");
            _diagnosticsLogService.Warning("Build index requested without a selected folder.");
            return;
        }

        StatusText.Text = "Status: scanning and indexing files...";
        BuildIndexButton.IsEnabled = false;

        try
        {
            _diagnosticsLogService.Info("Index build started.", _selectedFolder);

            var files = _fileCrawlerService.GetSupportedFiles(_selectedFolder).ToList();
            _diagnosticsLogService.Info($"Discovered {files.Count} supported file(s).", _selectedFolder);

            await _searchIndexService.BuildIndexAsync(files);

            _folderWatcherService.Start(_selectedFolder);

            StatusText.Text = $"Status: index built successfully ({files.Count} files indexed). Auto-monitoring is now active.";
            _diagnosticsLogService.Info($"Index build completed successfully. {files.Count} file(s) indexed.", _selectedFolder);
            _diagnosticsLogService.Info("Automatic folder monitoring started.", _selectedFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Indexing error");
            StatusText.Text = "Status: indexing failed";
            _diagnosticsLogService.Error($"Indexing failed: {ex.Message}", _selectedFolder ?? "");
        }
        finally
        {
            BuildIndexButton.IsEnabled = true;
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        string query = SearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            MessageBox.Show("Please type a search term.");
            _diagnosticsLogService.Warning("Search attempted with empty query.");
            return;
        }

        var request = new SearchRequest
        {
            QueryText = query,
            AllowedExtensions = GetAllowedExtensions(),
            Scope = GetSearchScope()
        };

        try
        {
            StatusText.Text = $"Status: searching for \"{query}\"...";
            _diagnosticsLogService.Info($"Search started for query: {query}");

            var results = await _searchIndexService.SearchAsync(request);

            ResultsGrid.ItemsSource = results;
            PreviewTitle.Text = "No document selected";
            PreviewText.Text = $"Found {results.Count} matching result(s). Select one to see details.";
            StatusText.Text = $"Status: found {results.Count} result(s)";

            _diagnosticsLogService.Info($"Search completed. {results.Count} result(s) returned.");

            int ocrResults = results.Count(r => r.OcrUsed);
            if (ocrResults > 0)
            {
                _diagnosticsLogService.Info($"{ocrResults} result(s) were extracted using OCR.");
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Status: search failed";
            _diagnosticsLogService.Error($"Search failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Search error");
        }
    }

    private void ResultsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsGrid.SelectedItem is SearchResultItem result)
        {
            PreviewTitle.Text = result.FileName;
            PreviewText.Text = _previewService.BuildPreview(result);

            _diagnosticsLogService.Info(
                $"Result selected. Method={result.ExtractionMethod}, OCR={(result.OcrUsed ? "Yes" : "No")}, Status={result.ExtractionStatus}",
                result.FilePath);
        }
    }

    private void OnFileChanged(FileChangeEvent changeEvent)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"Status: detected {changeEvent.ChangeType.ToString().ToLower()} change in {System.IO.Path.GetFileName(changeEvent.FullPath)}";
            _diagnosticsLogService.Info(
                $"Detected {changeEvent.ChangeType.ToString().ToLower()} file change.",
                changeEvent.FullPath);
        });

        _incrementalIndexService.QueueChange(changeEvent);
    }

    private void ClearDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        _diagnosticsLogService.Entries.Clear();
        _diagnosticsLogService.Info("Diagnostics log cleared.");
    }

    private void ResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsGrid.SelectedItem is not SearchResultItem item)
            return;

        if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
        {
            MessageBox.Show(
                "The selected file could not be found.",
                "Open File",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not open file:\n{ex.Message}",
                "Open File",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private List<string> GetAllowedExtensions()
    {
        var extensions = new List<string>();

        if (WordCheckBox.IsChecked == true)
        {
            extensions.Add(".docx");
            extensions.Add(".doc");
        }

        if (PdfCheckBox.IsChecked == true)
        {
            extensions.Add(".pdf");
        }

        if (ImageCheckBox.IsChecked == true)
        {
            extensions.Add(".jpg");
            extensions.Add(".jpeg");
            extensions.Add(".png");
        }

        if (TextCheckBox.IsChecked == true)
        {
            extensions.Add(".txt");
            extensions.Add(".md");
            extensions.Add(".csv");
            extensions.Add(".tex");
            extensions.Add(".cls");
            extensions.Add(".bib");
            extensions.Add(".py");
            extensions.Add(".html");
            extensions.Add(".htm");
            extensions.Add(".do");
            extensions.Add(".r");
            extensions.Add(".rda");
            extensions.Add(".ipynb");
            extensions.Add(".ado");
            extensions.Add(".sps");
            extensions.Add(".sas");
            extensions.Add(".m");
            extensions.Add(".json");
            extensions.Add(".css");
            extensions.Add(".xlsx");
            extensions.Add(".xls");
        }
        
        if (PptxCheckBox.IsChecked == true)
        {
            extensions.Add(".pptx");
            extensions.Add(".ppt");
        }
        return extensions;
    }

    private SearchScope GetSearchScope()
    {
        if (TitleOnlyRadio.IsChecked == true)
            return SearchScope.TitleOnly;

        if (DocumentTextRadio.IsChecked == true)
            return SearchScope.DocumentText;

        return SearchScope.AllFields;
    }

    protected override void OnClosed(EventArgs e)
    {
        _folderWatcherService.Dispose();
        _incrementalIndexService.Dispose();

        _diagnosticsLogService.Info("Application closing.");

        base.OnClosed(e);
    }
}