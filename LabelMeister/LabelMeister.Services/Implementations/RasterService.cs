using LabelMeister.Core.Models;
using LabelMeister.Core.Services;

namespace LabelMeister.Services.Implementations;

public class RasterService : IRasterService
{
    public RasterGridModel CreateGrid(double canvasWidth, double canvasHeight, int rows, int columns)
    {
        var grid = new RasterGridModel
        {
            Rows = rows,
            Columns = columns,
            CanvasWidth = canvasWidth,
            CanvasHeight = canvasHeight
        };

        // Initialize row positions (evenly distributed)
        for (int i = 0; i <= rows; i++)
        {
            grid.RowPositions.Add((canvasHeight / rows) * i);
        }

        // Initialize column positions (evenly distributed)
        for (int i = 0; i <= columns; i++)
        {
            grid.ColumnPositions.Add((canvasWidth / columns) * i);
        }

        return grid;
    }

    public void UpdateGridLine(RasterGridModel grid, bool isRow, int index, double position)
    {
        if (isRow)
        {
            if (index >= 0 && index < grid.RowPositions.Count)
            {
                grid.RowPositions[index] = Math.Clamp(position, 0, grid.CanvasHeight);
            }
        }
        else
        {
            if (index >= 0 && index < grid.ColumnPositions.Count)
            {
                grid.ColumnPositions[index] = Math.Clamp(position, 0, grid.CanvasWidth);
            }
        }
    }

    public List<CutoutRegionModel> GenerateCutoutRegions(RasterGridModel grid)
    {
        var regions = new List<CutoutRegionModel>();
        int id = 0;

        for (int row = 0; row < grid.Rows; row++)
        {
            for (int col = 0; col < grid.Columns; col++)
            {
                var x = grid.ColumnPositions[col];
                var y = grid.RowPositions[row];
                var width = grid.ColumnPositions[col + 1] - grid.ColumnPositions[col];
                var height = grid.RowPositions[row + 1] - grid.RowPositions[row];

                regions.Add(new CutoutRegionModel
                {
                    Id = id++,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    CombinedCellIds = new List<int> { id - 1 }
                });
            }
        }

        return regions;
    }
}

