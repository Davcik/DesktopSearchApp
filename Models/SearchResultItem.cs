namespace DesktopSearchApp.Models;

public sealed class SearchResultItem
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public string Title { get; set; } = "";
    public string DocumentText { get; set; } = "";
    public string Snippet { get; set; } = "";
    public bool OcrUsed { get; set; }
    public string ExtractionMethod { get; set; } = "";
    public int ExtractedTextLength { get; set; }
    public string ExtractionStatus { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public DateTime? LastModified { get; set; }
}