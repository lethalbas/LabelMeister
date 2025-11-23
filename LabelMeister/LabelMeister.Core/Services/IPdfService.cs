using LabelMeister.Core.Models;

namespace LabelMeister.Core.Services;

/// <summary>
/// Service for reading and writing PDFs
/// </summary>
public interface IPdfService
{
    Task<PdfDocumentModel?> LoadPdfAsync(string filePath);
    Task<byte[]?> RasterizePageAsync(PdfDocumentModel document, int pageIndex = 0, double dpi = 300.0);
    Task<bool> ExportToPdfAsync(List<PlacementModel> placements, List<CutoutRegionModel> cutouts, StripModel strip, ExportSettingsModel settings);
}

