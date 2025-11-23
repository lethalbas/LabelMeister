namespace LabelMeister.Core.Models;

/// <summary>
/// Template for saving/loading reusable flow configurations (future feature)
/// </summary>
public class TemplateModel
{
    public string Name { get; set; } = string.Empty;
    public RasterGridModel? Grid { get; set; }
    public List<CutoutRegionModel>? Cutouts { get; set; }
    public StripModel? Strip { get; set; }
    public List<PlacementModel>? Placements { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
}

