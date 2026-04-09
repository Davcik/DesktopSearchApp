using DesktopSearchApp.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using System.IO;
using PdfPigPage = UglyToad.PdfPig.Content.Page;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace DesktopSearchApp.Services;

public sealed class DocumentExtractionService
{
    private readonly OcrService _ocrService;
    private readonly PdfOcrService _pdfOcrService;
    private readonly PdfTextQualityService _pdfTextQualityService;

    public DocumentExtractionService()
    {
        _ocrService = new OcrService();
        _pdfOcrService = new PdfOcrService();
        _pdfTextQualityService = new PdfTextQualityService();
    }

    public IndexedDocument Extract(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".txt" => ExtractTextFile(filePath, "PlainText"),
                ".md" => ExtractTextFile(filePath, "PlainText"),
                ".csv" => ExtractTextFile(filePath, "PlainText"),
                ".tex" => ExtractTextFile(filePath, "PlainText"),
                ".cls" => ExtractTextFile(filePath, "PlainText"),
                ".bib" => ExtractTextFile(filePath, "PlainText"),
                ".py" => ExtractTextFile(filePath, "PlainText"),
                ".html" => ExtractTextFile(filePath, "PlainText"),
                ".do" => ExtractTextFile(filePath, "PlainText"),
                ".r" => ExtractTextFile(filePath, "PlainText"),
                ".ado" => ExtractTextFile(filePath, "PlainText"),
                ".sps" => ExtractTextFile(filePath, "PlainText"),
                ".sas" => ExtractTextFile(filePath, "PlainText"),
                ".m" => ExtractTextFile(filePath, "PlainText"),

                ".docx" => ExtractDocx(filePath),
                ".xlsx" => ExtractXlsx(filePath),
                ".pdf" => ExtractPdf(filePath),
                ".pptx" => ExtractPptx(filePath),

                ".jpg" => ExtractImageWithOcr(filePath),
                ".jpeg" => ExtractImageWithOcr(filePath),
                ".png" => ExtractImageWithOcr(filePath),

                ".doc" => ExtractFallbackByFileName(filePath, "Fallback"),
                ".dta" => ExtractFallbackByFileName(filePath, "Fallback"),
                ".epub" => ExtractFallbackByFileName(filePath, "Fallback"),
                ".sav" => ExtractFallbackByFileName(filePath, "Fallback"),
                ".ipynb" => ExtractIpynb(filePath),
                ".rda" => ExtractFallbackByFileName(filePath, "Fallback"),
                ".sas7bdat" => ExtractFallbackByFileName(filePath, "Fallback"),
                ".mat" => ExtractFallbackByFileName(filePath, "Fallback"),
                ".ppt" => ExtractFallbackByFileName(filePath, "LegacyPptFallback"),

                _ => ExtractFallbackByFileName(filePath, "Fallback")
            };
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, "UnhandledError", ex.Message);
        }
    }

    private IndexedDocument ExtractTextFile(string filePath, string method)
    {
        try
        {
            string text = File.ReadAllText(filePath);

            return BuildDocument(filePath, text, false, method, "Success", "");
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, method, ex.Message);
        }
    }

    private IndexedDocument ExtractIpynb(string filePath)
    {
        try
        {
            string text = File.ReadAllText(filePath);
            return BuildDocument(filePath, text, false, "IpynbText", "Success", "");
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, "IpynbText", ex.Message);
        }
    }

    private IndexedDocument ExtractDocx(string filePath)
    {
        try
        {
            string title = Path.GetFileNameWithoutExtension(filePath);
            string bodyText = "";

            using var doc = WordprocessingDocument.Open(filePath, false);
            var packageProps = doc.PackageProperties;

            if (!string.IsNullOrWhiteSpace(packageProps.Title))
                title = packageProps.Title;

            bodyText = doc.MainDocumentPart?.Document?.InnerText ?? "";

            var result = BuildDocument(filePath, bodyText, false, "OpenXmlDocx", "Success", "");
            result.Title = title;
            return result;
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, "OpenXmlDocx", ex.Message);
        }
    }

    private IndexedDocument ExtractXlsx(string filePath)
    {
        try
        {
            string title = Path.GetFileNameWithoutExtension(filePath);
            var textBuilder = new StringBuilder();

            using var document = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = document.WorkbookPart;

            if (document.PackageProperties?.Title is string packageTitle &&
                !string.IsNullOrWhiteSpace(packageTitle))
            {
                title = packageTitle;
            }

            if (workbookPart?.Workbook?.Sheets != null)
            {
                foreach (Sheet sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
                {
                    textBuilder.AppendLine($"[Sheet] {sheet.Name}");

                    var worksheetPart = workbookPart.GetPartById(sheet.Id!) as WorksheetPart;
                    if (worksheetPart?.Worksheet == null)
                        continue;

                    var rows = worksheetPart.Worksheet.Descendants<Row>();

                    foreach (var row in rows)
                    {
                        foreach (var cell in row.Elements<Cell>())
                        {
                            string value = GetCellValue(document, cell);
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                textBuilder.Append(value);
                                textBuilder.Append('\t');
                            }
                        }

                        textBuilder.AppendLine();
                    }

                    textBuilder.AppendLine();
                }
            }

            var result = BuildDocument(filePath, textBuilder.ToString(), false, "OpenXmlXlsx", "Success", "");
            result.Title = title;
            return result;
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, "OpenXmlXlsx", ex.Message);
        }
    }

    private string GetCellValue(SpreadsheetDocument document, Cell cell)
    {
        if (cell.CellValue == null)
            return string.Empty;

        string value = cell.CellValue.InnerText;

        if (cell.DataType == null)
            return value;

        if (cell.DataType.Value == CellValues.SharedString)
        {
            var stringTable = document.WorkbookPart?.SharedStringTablePart?.SharedStringTable;
            if (stringTable == null)
                return value;

            if (int.TryParse(value, out int index))
                return stringTable.ElementAt(index).InnerText;
        }

        return value;
    }

    private IndexedDocument ExtractPdf(string filePath)
    {
        try
        {
            string extractedText = "";
            bool ocrUsed = false;

            var textBuilder = new StringBuilder();

            try
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath);

                foreach (PdfPigPage page in pdf.GetPages())
                {
                    textBuilder.AppendLine(page.Text);
                    textBuilder.AppendLine();
                }
            }
            catch
            {
            }

            extractedText = textBuilder.ToString().Trim();
            string method = "PdfPigText";

            if (_pdfTextQualityService.ShouldUseOcrFallback(extractedText))
            {
                string ocrText = _pdfOcrService.ExtractTextFromPdfUsingOcr(filePath);

                if (!string.IsNullOrWhiteSpace(ocrText))
                {
                    extractedText = ocrText;
                    ocrUsed = true;
                    method = "PdfOcrFallback";
                }
            }

            return BuildDocument(filePath, extractedText, ocrUsed, method, "Success", "");
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, "PdfExtraction", ex.Message);
        }
    }

    private IndexedDocument ExtractImageWithOcr(string filePath)
    {
        try
        {
            string ocrText = _ocrService.ExtractTextFromImage(filePath);
            return BuildDocument(filePath, ocrText, true, "ImageOcr", "Success", "");
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, "ImageOcr", ex.Message);
        }
    }

    private IndexedDocument ExtractFallbackByFileName(string filePath, string method)
    {
        return BuildDocument(filePath, "", false, method, "FallbackOnly", "");
    }

    private IndexedDocument BuildDocument(
        string filePath,
        string documentText,
        bool ocrUsed,
        string method,
        string status,
        string errorMessage)
    {
        return new IndexedDocument
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Extension = Path.GetExtension(filePath).ToLowerInvariant(),
            Title = Path.GetFileNameWithoutExtension(filePath),
            DocumentText = documentText ?? "",
            OcrUsed = ocrUsed,
            Diagnostics = new ExtractionDiagnostics
            {
                ExtractionMethod = method,
                OcrUsed = ocrUsed,
                ExtractedTextLength = documentText?.Length ?? 0,
                Status = status,
                ErrorMessage = errorMessage ?? ""
            }
        };
    }

    private IndexedDocument BuildErrorDocument(string filePath, string method, string errorMessage)
    {
        return new IndexedDocument
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Extension = Path.GetExtension(filePath).ToLowerInvariant(),
            Title = Path.GetFileNameWithoutExtension(filePath),
            DocumentText = "",
            OcrUsed = false,
            Diagnostics = new ExtractionDiagnostics
            {
                ExtractionMethod = method,
                OcrUsed = false,
                ExtractedTextLength = 0,
                Status = "Error",
                ErrorMessage = errorMessage ?? ""
            }
        };
    }

    private IndexedDocument ExtractPptx(string filePath)
    {
        try
        {
            string title = Path.GetFileNameWithoutExtension(filePath);
            var textBuilder = new StringBuilder();

            using var presentation = PresentationDocument.Open(filePath, false);

            if (presentation.PackageProperties?.Title is string packageTitle &&
                !string.IsNullOrWhiteSpace(packageTitle))
            {
                title = packageTitle;
            }

            var presentationPart = presentation.PresentationPart;
            var slideIdList = presentationPart?.Presentation?.SlideIdList;

            if (presentationPart != null && slideIdList != null)
            {
                int slideNumber = 0;

                foreach (P.SlideId slideId in slideIdList.Elements<P.SlideId>())
                {
                    slideNumber++;

                    var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                    textBuilder.AppendLine($"[Slide {slideNumber}]");

                    if (slidePart.Slide != null)
                    {
                        foreach (A.Paragraph paragraph in slidePart.Slide.Descendants<A.Paragraph>())
                        {
                            var paragraphText = new StringBuilder();

                            foreach (A.Text text in paragraph.Descendants<A.Text>())
                            {
                                if (!string.IsNullOrWhiteSpace(text.Text))
                                    paragraphText.Append(text.Text);
                            }

                            if (paragraphText.Length > 0)
                                textBuilder.AppendLine(paragraphText.ToString());
                        }
                    }

                    textBuilder.AppendLine();
                }
            }

            var result = BuildDocument(filePath, textBuilder.ToString(), false, "OpenXmlPptx", "Success", "");
            result.Title = title;
            return result;
        }
        catch (Exception ex)
        {
            return BuildErrorDocument(filePath, "OpenXmlPptx", ex.Message);
        }
    }
}