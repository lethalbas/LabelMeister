namespace LabelMeister.Core.Models;

/// <summary>
/// Settings for exporting the final PDF
/// </summary>
public class ExportSettingsModel
{
    public string OutputPath { get; set; } = string.Empty;
    public string FileName { get; set; } = "labels.pdf";
    public double Dpi { get; set; } = 300.0;
    public bool IncludeCutLines { get; set; } = true;
    public double CutLineWidth { get; set; } = 0.1; // in mm
}

