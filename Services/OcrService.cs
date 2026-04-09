using Tesseract;
using System.IO;

namespace DesktopSearchApp.Services;

public sealed class OcrService
{
    private readonly string _tessdataPath;

    public OcrService()
    {
        _tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    }

    public string ExtractTextFromImage(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        try
        {
            using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
            using var image = Pix.LoadFromFile(filePath);
            using var page = engine.Process(image);

            return page.GetText()?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
