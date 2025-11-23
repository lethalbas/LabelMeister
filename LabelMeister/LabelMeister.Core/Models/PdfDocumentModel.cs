namespace LabelMeister.Core.Models;

/// <summary>
/// Represents a PDF document with its metadata and page data
/// </summary>
public class PdfDocumentModel
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public byte[]? RasterizedImageData { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Dpi { get; set; } = 300.0;
}

