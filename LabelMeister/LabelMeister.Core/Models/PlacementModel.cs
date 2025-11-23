namespace LabelMeister.Core.Models;

/// <summary>
/// Represents a placed cutout on the strip with position, rotation, and scale
/// </summary>
public class PlacementModel
{
    public int CutoutId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; } // in degrees (0, 90, 180, 270)
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public int ZIndex { get; set; } // for layering
}

