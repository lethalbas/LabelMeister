using LabelMeister.Core.Models;

namespace LabelMeister.Core.Services;

/// <summary>
/// Service for exporting the final layout to PDF
/// </summary>
public interface IExportService
{
    Task<bool> ExportAsync(List<PlacementModel> placements, List<CutoutRegionModel> cutouts, StripModel strip, ExportSettingsModel settings);
    byte[]? GeneratePreview(List<PlacementModel> placements, List<CutoutRegionModel> cutouts, StripModel strip);
}

