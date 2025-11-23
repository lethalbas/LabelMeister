using LabelMeister.Core.Models;

namespace LabelMeister.Core.Services;

/// <summary>
/// Service for managing strip dimensions
/// </summary>
public interface IStripService
{
    List<StripModel> GetPredefinedStrips();
    StripModel CreateCustomStrip(double width, double height, bool isLandscape);
    double ConvertMmToPixels(double mm, double dpi);
    double ConvertPixelsToMm(double pixels, double dpi);
}

