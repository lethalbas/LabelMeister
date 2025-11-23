using LabelMeister.Core.Models;

namespace LabelMeister.Core.Services;

/// <summary>
/// Service for generating and managing grid overlays
/// </summary>
public interface IRasterService
{
    RasterGridModel CreateGrid(double canvasWidth, double canvasHeight, int rows, int columns);
    void UpdateGridLine(RasterGridModel grid, bool isRow, int index, double position);
    List<CutoutRegionModel> GenerateCutoutRegions(RasterGridModel grid);
}

