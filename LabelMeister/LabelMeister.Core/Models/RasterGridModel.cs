namespace LabelMeister.Core.Models;

/// <summary>
/// Defines a grid overlay with rows and columns
/// </summary>
public class RasterGridModel
{
    public int Rows { get; set; } = 3;
    public int Columns { get; set; } = 3;
    public List<double> RowPositions { get; set; } = new();
    public List<double> ColumnPositions { get; set; } = new();
    public double CanvasWidth { get; set; }
    public double CanvasHeight { get; set; }
}

