using PDFiumSharp;
using PDFiumSharp.Types;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Tesseract;
using System.IO;

namespace DesktopSearchApp.Services;

public sealed class PdfOcrService
{
    private readonly string _tessdataPath;

    public PdfOcrService()
    {
        _tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    }

    public string ExtractTextFromPdfUsingOcr(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        var allText = new StringBuilder();
        string tempFolder = Path.Combine(Path.GetTempPath(), "DesktopSearchAppPdfOcr");

        Directory.CreateDirectory(tempFolder);

        try
        {
            using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
            using var document = new PdfDocument(filePath);

            for (int i = 0; i < document.Pages.Count; i++)
            {
                var page = document.Pages[i];

                int width = Math.Max((int)page.Width, 1);
                int height = Math.Max((int)page.Height, 1);

                using var pdfBitmap = new PDFiumBitmap(width * 2, height * 2, true);
                page.Render(pdfBitmap);

                string tempImagePath = Path.Combine(tempFolder, $"page_{i + 1}.png");
                // Save the PDFiumBitmap directly to file
                pdfBitmap.Save(tempImagePath, 96, 96);

                using var pix = Pix.LoadFromFile(tempImagePath);
                using var ocrPage = engine.Process(pix);

                string pageText = ocrPage.GetText() ?? "";
                allText.AppendLine(pageText);
                allText.AppendLine();

                try
                {
                    File.Delete(tempImagePath);
                }
                catch
                {
                }
            }
        }
        catch
        {
            return string.Empty;
        }

        return allText.ToString().Trim();
    }
}
