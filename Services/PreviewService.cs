using DesktopSearchApp.Models;
using System.Text;

namespace DesktopSearchApp.Services;

public sealed class PreviewService
{
    public string BuildPreview(SearchResultItem result)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"File: {result.FileName}");
        builder.AppendLine($"Path: {result.FilePath}");
        builder.AppendLine($"Type: {result.Extension}");
        builder.AppendLine($"OCR Used: {(result.OcrUsed ? "Yes" : "No")}");
        builder.AppendLine($"Method: {result.ExtractionMethod}");
        builder.AppendLine($"Status: {result.ExtractionStatus}");
        builder.AppendLine($"Text Length: {result.ExtractedTextLength}");

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            builder.AppendLine($"Error: {result.ErrorMessage}");
        }

        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(result.Title))
        {
            builder.AppendLine("Title:");
            builder.AppendLine(result.Title);
            builder.AppendLine();
        }

        builder.AppendLine("Snippet:");
        builder.AppendLine(result.Snippet);
        builder.AppendLine();

        builder.AppendLine("Preview Text:");
        builder.AppendLine(BuildBodyPreview(result.DocumentText));

        return builder.ToString().Trim();
    }

    private static string BuildBodyPreview(string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            return "No extracted text available.";

        string normalized = documentText.Replace("\r\n", "\n").Trim();

        if (normalized.Length <= 1200)
            return normalized;

        return normalized[..1200] + "...";
    }
}
