using LabelMeister.Core.Models;
using LabelMeister.Services.Implementations;
using Xunit;

namespace LabelMeister.Tests;

public class CutoutServiceTests
{
    private readonly CutoutService _cutoutService;

    public CutoutServiceTests()
    {
        _cutoutService = new CutoutService();
    }

    #region CombineRegions Tests

    [Fact]
    public void CombineRegions_EmptyList_ThrowsArgumentException()
    {
        // Arrange
        var regions = new List<CutoutRegionModel>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cutoutService.CombineRegions(regions));
    }

    [Fact]
    public void CombineRegions_SingleRegion_ReturnsCombinedRegion()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 10,
            Y = 20,
            Width = 100,
            Height = 200,
            CombinedCellIds = new List<int> { 1, 2 }
        };
        var regions = new List<CutoutRegionModel> { region };

        // Act
        var result = _cutoutService.CombineRegions(regions);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.X);
        Assert.Equal(20, result.Y);
        Assert.Equal(100, result.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(new List<int> { 1, 2 }, result.CombinedCellIds);
    }

    [Fact]
    public void CombineRegions_TwoRegions_ReturnsCorrectBoundingBox()
    {
        // Arrange
        var region1 = new CutoutRegionModel
        {
            Id = 1,
            X = 10,
            Y = 20,
            Width = 50,
            Height = 60,
            CombinedCellIds = new List<int> { 1 }
        };
        var region2 = new CutoutRegionModel
        {
            Id = 2,
            X = 70,
            Y = 30,
            Width = 40,
            Height = 50,
            CombinedCellIds = new List<int> { 2 }
        };
        var regions = new List<CutoutRegionModel> { region1, region2 };

        // Act
        var result = _cutoutService.CombineRegions(regions);

        // Assert
        Assert.Equal(1, result.Id); // Should use first region's ID
        Assert.Equal(10, result.X); // Min X
        Assert.Equal(20, result.Y); // Min Y
        Assert.Equal(100, result.Width); // Max X (70 + 40) - Min X (10)
        Assert.Equal(80, result.Height); // Max Y (30 + 50) - Min Y (20)
        Assert.Contains(1, result.CombinedCellIds);
        Assert.Contains(2, result.CombinedCellIds);
    }

    [Fact]
    public void CombineRegions_MultipleRegions_CombinesAllCellIds()
    {
        // Arrange
        var region1 = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            CombinedCellIds = new List<int> { 1, 2 }
        };
        var region2 = new CutoutRegionModel
        {
            Id = 2,
            X = 20,
            Y = 20,
            Width = 10,
            Height = 10,
            CombinedCellIds = new List<int> { 3, 4 }
        };
        var region3 = new CutoutRegionModel
        {
            Id = 3,
            X = 40,
            Y = 40,
            Width = 10,
            Height = 10,
            CombinedCellIds = new List<int> { 2, 5 } // 2 is duplicate
        };
        var regions = new List<CutoutRegionModel> { region1, region2, region3 };

        // Act
        var result = _cutoutService.CombineRegions(regions);

        // Assert
        Assert.Equal(5, result.CombinedCellIds.Count);
        Assert.Contains(1, result.CombinedCellIds);
        Assert.Contains(2, result.CombinedCellIds);
        Assert.Contains(3, result.CombinedCellIds);
        Assert.Contains(4, result.CombinedCellIds);
        Assert.Contains(5, result.CombinedCellIds);
    }

    [Fact]
    public void CombineRegions_OverlappingRegions_ReturnsCorrectBoundingBox()
    {
        // Arrange
        var region1 = new CutoutRegionModel
        {
            Id = 1,
            X = 10,
            Y = 10,
            Width = 100,
            Height = 100,
            CombinedCellIds = new List<int> { 1 }
        };
        var region2 = new CutoutRegionModel
        {
            Id = 2,
            X = 50,
            Y = 50,
            Width = 100,
            Height = 100,
            CombinedCellIds = new List<int> { 2 }
        };
        var regions = new List<CutoutRegionModel> { region1, region2 };

        // Act
        var result = _cutoutService.CombineRegions(regions);

        // Assert
        Assert.Equal(10, result.X);
        Assert.Equal(10, result.Y);
        Assert.Equal(140, result.Width); // (50 + 100) - 10
        Assert.Equal(140, result.Height); // (50 + 100) - 10
    }

    [Fact]
    public void CombineRegions_RegionsWithNegativeCoordinates_HandlesCorrectly()
    {
        // Arrange
        var region1 = new CutoutRegionModel
        {
            Id = 1,
            X = -10,
            Y = -20,
            Width = 50,
            Height = 60,
            CombinedCellIds = new List<int> { 1 }
        };
        var region2 = new CutoutRegionModel
        {
            Id = 2,
            X = 30,
            Y = 40,
            Width = 20,
            Height = 30,
            CombinedCellIds = new List<int> { 2 }
        };
        var regions = new List<CutoutRegionModel> { region1, region2 };

        // Act
        var result = _cutoutService.CombineRegions(regions);

        // Assert
        Assert.Equal(-10, result.X);
        Assert.Equal(-20, result.Y);
        Assert.Equal(60, result.Width); // (30 + 20) - (-10)
        Assert.Equal(90, result.Height); // (40 + 30) - (-20)
    }

    #endregion

    #region DiscardRegion Tests

    [Fact]
    public void DiscardRegion_SetsIsDiscardedToTrue()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            IsDiscarded = false
        };

        // Act
        _cutoutService.DiscardRegion(region);

        // Assert
        Assert.True(region.IsDiscarded);
    }

    [Fact]
    public void DiscardRegion_AlreadyDiscarded_RemainsDiscarded()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            IsDiscarded = true
        };

        // Act
        _cutoutService.DiscardRegion(region);

        // Assert
        Assert.True(region.IsDiscarded);
    }

    #endregion

    #region RestoreRegion Tests

    [Fact]
    public void RestoreRegion_SetsIsDiscardedToFalse()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            IsDiscarded = true
        };

        // Act
        _cutoutService.RestoreRegion(region);

        // Assert
        Assert.False(region.IsDiscarded);
    }

    [Fact]
    public void RestoreRegion_AlreadyRestored_RemainsRestored()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            IsDiscarded = false
        };

        // Act
        _cutoutService.RestoreRegion(region);

        // Assert
        Assert.False(region.IsDiscarded);
    }

    #endregion

    #region GetAdjacentRegions Tests

    [Fact]
    public void GetAdjacentRegions_NoAdjacentRegions_ReturnsEmptyList()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var other = new CutoutRegionModel
        {
            Id = 2,
            X = 100,
            Y = 100,
            Width = 10,
            Height = 10
        };
        var allRegions = new List<CutoutRegionModel> { region, other };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAdjacentRegions_ExcludesSelf()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var allRegions = new List<CutoutRegionModel> { region };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAdjacentRegions_RightEdgeTouching_ReturnsAdjacent()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var adjacent = new CutoutRegionModel
        {
            Id = 2,
            X = 10, // Right edge touches left edge
            Y = 0,
            Width = 10,
            Height = 10
        };
        var allRegions = new List<CutoutRegionModel> { region, adjacent };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void GetAdjacentRegions_LeftEdgeTouching_ReturnsAdjacent()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 10,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var adjacent = new CutoutRegionModel
        {
            Id = 2,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10 // Right edge touches left edge of region
        };
        var allRegions = new List<CutoutRegionModel> { region, adjacent };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void GetAdjacentRegions_BottomEdgeTouching_ReturnsAdjacent()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var adjacent = new CutoutRegionModel
        {
            Id = 2,
            X = 0,
            Y = 10, // Bottom edge touches top edge
            Width = 10,
            Height = 10
        };
        var allRegions = new List<CutoutRegionModel> { region, adjacent };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void GetAdjacentRegions_TopEdgeTouching_ReturnsAdjacent()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 10,
            Width = 10,
            Height = 10
        };
        var adjacent = new CutoutRegionModel
        {
            Id = 2,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10 // Bottom edge touches top edge of region
        };
        var allRegions = new List<CutoutRegionModel> { region, adjacent };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void GetAdjacentRegions_WithTolerance_ReturnsAdjacent()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var adjacent = new CutoutRegionModel
        {
            Id = 2,
            X = 10.5, // Within tolerance (1.0)
            Y = 0,
            Width = 10,
            Height = 10
        };
        var allRegions = new List<CutoutRegionModel> { region, adjacent };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void GetAdjacentRegions_OverlappingButNotTouching_ReturnsEmpty()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var overlapping = new CutoutRegionModel
        {
            Id = 2,
            X = 5, // Overlaps but doesn't touch edge
            Y = 5,
            Width = 10,
            Height = 10
        };
        var allRegions = new List<CutoutRegionModel> { region, overlapping };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAdjacentRegions_VerticalEdgeButNoOverlap_ReturnsEmpty()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        var nonAdjacent = new CutoutRegionModel
        {
            Id = 2,
            X = 10, // Right edge touches
            Y = 20, // But no vertical overlap
            Width = 10,
            Height = 10
        };
        var allRegions = new List<CutoutRegionModel> { region, nonAdjacent };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAdjacentRegions_MultipleAdjacentRegions_ReturnsAll()
    {
        // Arrange
        var region = new CutoutRegionModel
        {
            Id = 1,
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10
        };
        var right = new CutoutRegionModel { Id = 2, X = 20, Y = 10, Width = 10, Height = 10 };
        var left = new CutoutRegionModel { Id = 3, X = 0, Y = 10, Width = 10, Height = 10 };
        var top = new CutoutRegionModel { Id = 4, X = 10, Y = 0, Width = 10, Height = 10 };
        var bottom = new CutoutRegionModel { Id = 5, X = 10, Y = 20, Width = 10, Height = 10 };
        var allRegions = new List<CutoutRegionModel> { region, right, left, top, bottom };

        // Act
        var result = _cutoutService.GetAdjacentRegions(region, allRegions);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains(right, result);
        Assert.Contains(left, result);
        Assert.Contains(top, result);
        Assert.Contains(bottom, result);
    }

    #endregion
}

