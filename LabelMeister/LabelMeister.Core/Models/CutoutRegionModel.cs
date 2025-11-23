namespace LabelMeister.Core.Models;

/// <summary>
/// Represents a single selected region from the PDF grid
/// </summary>
public class CutoutRegionModel
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsSelected { get; set; }
    public bool IsDiscarded { get; set; }
    public List<int> CombinedCellIds { get; set; } = new();
    public byte[]? ImageData { get; set; }
}

