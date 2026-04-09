namespace DesktopSearchApp.Services;

public sealed class PdfTextQualityService
{
    public bool ShouldUseOcrFallback(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return true;

        string normalized = extractedText.Trim();

        if (normalized.Length < 80)
            return true;

        int letterCount = normalized.Count(char.IsLetter);
        if (letterCount < 30)
            return true;

        return false;
    }
}
