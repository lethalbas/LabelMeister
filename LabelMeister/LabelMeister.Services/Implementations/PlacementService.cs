using LabelMeister.Core.Models;
using LabelMeister.Core.Services;

namespace LabelMeister.Services.Implementations;

public class PlacementService : IPlacementService
{
    public PlacementModel CreatePlacement(int cutoutId, double x, double y)
    {
        return new PlacementModel
        {
            CutoutId = cutoutId,
            X = x,
            Y = y,
            Rotation = 0,
            ScaleX = 1.0,
            ScaleY = 1.0,
            ZIndex = 0
        };
    }

    public void UpdatePlacement(PlacementModel placement, double x, double y)
    {
        placement.X = x;
        placement.Y = y;
    }

    public void RotatePlacement(PlacementModel placement, double rotation)
    {
        // Normalize rotation to 0, 90, 180, 270
        placement.Rotation = ((int)Math.Round(rotation / 90.0) * 90) % 360;
        if (placement.Rotation < 0)
            placement.Rotation += 360;
    }

    public void ScalePlacement(PlacementModel placement, double scaleX, double scaleY)
    {
        placement.ScaleX = Math.Max(0.1, scaleX);
        placement.ScaleY = Math.Max(0.1, scaleY);
    }

    public bool IsPlacementValid(PlacementModel placement, CutoutRegionModel cutout, StripModel strip)
    {
        var stripWidthPx = ConvertMmToPixels(strip.Width, strip.Dpi);
        var stripHeightPx = ConvertMmToPixels(strip.Height, strip.Dpi);

        var cutoutWidthPx = cutout.Width * placement.ScaleX;
        var cutoutHeightPx = cutout.Height * placement.ScaleY;

        // Check if placement fits within strip bounds
        return placement.X >= 0 &&
               placement.Y >= 0 &&
               placement.X + cutoutWidthPx <= stripWidthPx &&
               placement.Y + cutoutHeightPx <= stripHeightPx;
    }

    private double ConvertMmToPixels(double mm, double dpi)
    {
        return mm * dpi / 25.4;
    }
}

