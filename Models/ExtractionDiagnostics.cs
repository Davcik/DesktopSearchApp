namespace DesktopSearchApp.Models;

public sealed class ExtractionDiagnostics
{
    public string ExtractionMethod { get; set; } = "";
    public bool OcrUsed { get; set; }
    public int ExtractedTextLength { get; set; }
    public string Status { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}
