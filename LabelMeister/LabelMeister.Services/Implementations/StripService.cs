using LabelMeister.Core.Models;
using LabelMeister.Core.Services;

namespace LabelMeister.Services.Implementations;

public class StripService : IStripService
{
    public List<StripModel> GetPredefinedStrips()
    {
        return PredefinedStrips.DefaultStrips;
    }

    public StripModel CreateCustomStrip(double width, double height, bool isLandscape)
    {
        return new StripModel
        {
            Name = "Custom",
            Width = width,
            Height = height,
            IsLandscape = isLandscape
        };
    }

    public double ConvertMmToPixels(double mm, double dpi)
    {
        return mm * dpi / 25.4; // 1 inch = 25.4 mm
    }

    public double ConvertPixelsToMm(double pixels, double dpi)
    {
        return pixels * 25.4 / dpi;
    }
}

