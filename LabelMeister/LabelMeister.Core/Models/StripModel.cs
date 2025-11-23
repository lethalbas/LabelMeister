namespace LabelMeister.Core.Models;

/// <summary>
/// Represents the target label strip dimensions
/// </summary>
public class StripModel
{
    public string Name { get; set; } = string.Empty;
    public double Width { get; set; } // in mm
    public double Height { get; set; } // in mm
    public bool IsLandscape { get; set; } = true;
    public double Dpi { get; set; } = 300.0;
}

/// <summary>
/// Predefined label strip sizes
/// </summary>
public static class PredefinedStrips
{
    public static readonly List<StripModel> DefaultStrips = new()
    {
        // Standard label printer widths (continuous)
        new StripModel { Name = "62mm Continuous", Width = 62, Height = 297, IsLandscape = false }, // Common thermal printer width
        new StripModel { Name = "50.8mm (2\") Continuous", Width = 50.8, Height = 297, IsLandscape = false },
        new StripModel { Name = "38.1mm (1.5\") Continuous", Width = 38.1, Height = 297, IsLandscape = false },
        new StripModel { Name = "25.4mm (1\") Continuous", Width = 25.4, Height = 297, IsLandscape = false },
        
        // Standard label sizes
        new StripModel { Name = "62x100mm", Width = 62, Height = 100, IsLandscape = false },
        new StripModel { Name = "62x150mm", Width = 62, Height = 150, IsLandscape = false },
        new StripModel { Name = "50.8x100mm (2\"x4\")", Width = 50.8, Height = 101.6, IsLandscape = false },
        new StripModel { Name = "38.1x88.9mm (1.5\"x3.5\")", Width = 38.1, Height = 88.9, IsLandscape = false },
        
        // Shipping label sizes
        new StripModel { Name = "100x150mm", Width = 100, Height = 150, IsLandscape = false },
        new StripModel { Name = "100x200mm", Width = 100, Height = 200, IsLandscape = false },
        
        // Small label sizes
        new StripModel { Name = "50x70mm", Width = 50, Height = 70, IsLandscape = false },
        new StripModel { Name = "70x50mm", Width = 70, Height = 50, IsLandscape = true },
        new StripModel { Name = "40x60mm", Width = 40, Height = 60, IsLandscape = false },
        
        // A4 and standard paper sizes
        new StripModel { Name = "A4 (210x297mm)", Width = 210, Height = 297, IsLandscape = false },
        new StripModel { Name = "A4 Landscape", Width = 297, Height = 210, IsLandscape = true },
        new StripModel { Name = "A5 (148x210mm)", Width = 148, Height = 210, IsLandscape = false },
        new StripModel { Name = "A6 (105x148mm)", Width = 105, Height = 148, IsLandscape = false },
        
        // Custom option
        new StripModel { Name = "Custom", Width = 100, Height = 150, IsLandscape = false }
    };
}

