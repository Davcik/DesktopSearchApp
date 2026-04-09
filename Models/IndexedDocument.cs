namespace DesktopSearchApp.Models;

public sealed class IndexedDocument
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public string Title { get; set; } = "";
    public string DocumentText { get; set; } = "";
    public bool OcrUsed { get; set; }
    public ExtractionDiagnostics Diagnostics { get; set; } = new();
}