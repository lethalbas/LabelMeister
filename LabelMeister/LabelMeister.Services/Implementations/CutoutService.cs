using LabelMeister.Core.Models;
using LabelMeister.Core.Services;

namespace LabelMeister.Services.Implementations;

public class CutoutService : ICutoutService
{
    public CutoutRegionModel CombineRegions(List<CutoutRegionModel> regions)
    {
        if (regions.Count == 0)
            throw new ArgumentException("Cannot combine empty regions list");

        if (!CanFormRectangle(regions))
            throw new ArgumentException("Selected regions do not form a valid 4-sided rectangle");

        var minX = regions.Min(r => r.X);
        var minY = regions.Min(r => r.Y);
        var maxX = regions.Max(r => r.X + r.Width);
        var maxY = regions.Max(r => r.Y + r.Height);

        var combinedIds = regions.SelectMany(r => r.CombinedCellIds).Distinct().ToList();

        return new CutoutRegionModel
        {
            Id = regions[0].Id,
            X = minX,
            Y = minY,
            Width = maxX - minX,
            Height = maxY - minY,
            CombinedCellIds = combinedIds
        };
    }

    private bool CanFormRectangle(List<CutoutRegionModel> regions)
    {
        if (regions.Count == 0) return false;
        if (regions.Count == 1) return true; // Single region is always a rectangle

        const double tolerance = 0.1; // Small tolerance for floating point comparisons

        // Calculate bounding box
        var minX = regions.Min(r => r.X);
        var minY = regions.Min(r => r.Y);
        var maxX = regions.Max(r => r.X + r.Width);
        var maxY = regions.Max(r => r.Y + r.Height);
        var boundingWidth = maxX - minX;
        var boundingHeight = maxY - minY;

        // Check if all regions are adjacent and form a contiguous rectangle
        // First, verify all regions are connected (adjacent)
        if (!AreAllConnected(regions))
            return false;

        // Check that the bounding box area matches the sum of region areas
        // For a valid rectangle, the sum of region areas should equal the bounding box area
        var totalArea = regions.Sum(r => r.Width * r.Height);
        var boundingArea = boundingWidth * boundingHeight;

        // For a perfect rectangle, total area should equal bounding area (no gaps or overlaps)
        if (Math.Abs(totalArea - boundingArea) > tolerance)
            return false;

        // Verify all regions align to grid boundaries (no partial overlaps)
        // Check that regions don't overlap (except at edges)
        for (int i = 0; i < regions.Count; i++)
        {
            for (int j = i + 1; j < regions.Count; j++)
            {
                var r1 = regions[i];
                var r2 = regions[j];

                // Check for overlap (not just touching)
                var overlapX = Math.Max(0, Math.Min(r1.X + r1.Width, r2.X + r2.Width) - Math.Max(r1.X, r2.X));
                var overlapY = Math.Max(0, Math.Min(r1.Y + r1.Height, r2.Y + r2.Height) - Math.Max(r1.Y, r2.Y));
                var overlapArea = overlapX * overlapY;

                // Overlap should be 0 (only touching at edges) or very small (tolerance)
                if (overlapArea > tolerance)
                    return false;
            }
        }

        return true;
    }

    private bool AreAllConnected(List<CutoutRegionModel> regions)
    {
        if (regions.Count <= 1) return true;

        // Use a graph connectivity check - all regions should be reachable from the first region
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0); // Start with first region
        visited.Add(0);

        const double tolerance = 1.0;

        while (queue.Count > 0)
        {
            var currentIdx = queue.Dequeue();
            var currentRegion = regions[currentIdx];

            // Check all other regions for adjacency
            for (int i = 0; i < regions.Count; i++)
            {
                if (visited.Contains(i)) continue;

                var other = regions[i];
                
                // Check if regions are adjacent (share an edge)
                bool isAdjacent =
                    (Math.Abs(other.X + other.Width - currentRegion.X) < tolerance &&
                     !(other.Y + other.Height <= currentRegion.Y || other.Y >= currentRegion.Y + currentRegion.Height)) ||
                    (Math.Abs(currentRegion.X + currentRegion.Width - other.X) < tolerance &&
                     !(currentRegion.Y + currentRegion.Height <= other.Y || currentRegion.Y >= other.Y + other.Height)) ||
                    (Math.Abs(other.Y + other.Height - currentRegion.Y) < tolerance &&
                     !(other.X + other.Width <= currentRegion.X || other.X >= currentRegion.X + currentRegion.Width)) ||
                    (Math.Abs(currentRegion.Y + currentRegion.Height - other.Y) < tolerance &&
                     !(currentRegion.X + currentRegion.Width <= other.X || currentRegion.X >= other.X + other.Width));

                if (isAdjacent)
                {
                    visited.Add(i);
                    queue.Enqueue(i);
                }
            }
        }

        // All regions should be visited (connected)
        return visited.Count == regions.Count;
    }

    public void DiscardRegion(CutoutRegionModel region)
    {
        region.IsDiscarded = true;
    }

    public void RestoreRegion(CutoutRegionModel region)
    {
        region.IsDiscarded = false;
    }

    public List<CutoutRegionModel> GetAdjacentRegions(CutoutRegionModel region, List<CutoutRegionModel> allRegions)
    {
        var adjacent = new List<CutoutRegionModel>();
        const double tolerance = 1.0; // pixels

        foreach (var other in allRegions)
        {
            if (other.Id == region.Id)
                continue;

            // Check if regions are adjacent (share an edge)
            bool isAdjacent = 
                (Math.Abs(other.X + other.Width - region.X) < tolerance && // Right edge of other touches left edge of region
                 !(other.Y + other.Height < region.Y || other.Y > region.Y + region.Height)) ||
                (Math.Abs(region.X + region.Width - other.X) < tolerance && // Right edge of region touches left edge of other
                 !(region.Y + region.Height < other.Y || region.Y > other.Y + other.Height)) ||
                (Math.Abs(other.Y + other.Height - region.Y) < tolerance && // Bottom edge of other touches top edge of region
                 !(other.X + other.Width < region.X || other.X > region.X + region.Width)) ||
                (Math.Abs(region.Y + region.Height - other.Y) < tolerance && // Bottom edge of region touches top edge of other
                 !(region.X + region.Width < other.X || region.X > other.X + other.Width));

            if (isAdjacent)
            {
                adjacent.Add(other);
            }
        }

        return adjacent;
    }
}

