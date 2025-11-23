using LabelMeister.Core.Models;

namespace LabelMeister.Core.Services;

/// <summary>
/// Service for managing cutout regions (combining, discarding)
/// </summary>
public interface ICutoutService
{
    CutoutRegionModel CombineRegions(List<CutoutRegionModel> regions);
    void DiscardRegion(CutoutRegionModel region);
    void RestoreRegion(CutoutRegionModel region);
    List<CutoutRegionModel> GetAdjacentRegions(CutoutRegionModel region, List<CutoutRegionModel> allRegions);
}

