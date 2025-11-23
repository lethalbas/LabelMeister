using LabelMeister.Core.Models;

namespace LabelMeister.Core.Services;

/// <summary>
/// Service for managing placement of cutouts on the strip
/// </summary>
public interface IPlacementService
{
    PlacementModel CreatePlacement(int cutoutId, double x, double y);
    void UpdatePlacement(PlacementModel placement, double x, double y);
    void RotatePlacement(PlacementModel placement, double rotation);
    void ScalePlacement(PlacementModel placement, double scaleX, double scaleY);
    bool IsPlacementValid(PlacementModel placement, CutoutRegionModel cutout, StripModel strip);
}

