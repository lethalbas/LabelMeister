using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using System.Windows.Media.Imaging;
using SkiaSharp;
using System.IO;
using System.Windows.Media;

namespace LabelMeister.ViewModels;

public partial class RasterizeViewModel : ObservableObject
{
    private readonly IRasterService _rasterService;

    [ObservableProperty]
    private PdfDocumentModel? _pdfDocument;

    [ObservableProperty]
    private RasterGridModel? _grid;

    [ObservableProperty]
    private int _rows = 3;

    [ObservableProperty]
    private int _columns = 3;

    [ObservableProperty]
    private bool _isCompleted = false;

    [ObservableProperty]
    private bool _isEnabled = false;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private double _previewScale = 1.0;

    [ObservableProperty]
    private double _previewOffsetX = 0;

    [ObservableProperty]
    private double _previewOffsetY = 0;

    public event Action<RasterGridModel>? GridCreated;
    public event Action<RasterGridModel>? GridUpdated;
    public event Action<bool>? CompletionChanged;

    public RasterizeViewModel(IRasterService rasterService)
    {
        _rasterService = rasterService;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        CompletionChanged?.Invoke(value);
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    public void SetPdfDocument(PdfDocumentModel pdf)
    {
        PdfDocument = pdf;
        if (pdf != null && pdf.RasterizedImageData != null)
        {
            CreateGrid();
        }
    }

    [RelayCommand]
    private void CreateGrid()
    {
        if (PdfDocument == null || !IsEnabled) return;

        var width = PdfDocument.Width;
        var height = PdfDocument.Height;

        Grid = _rasterService.CreateGrid(width, height, Rows, Columns);
        GridCreated?.Invoke(Grid);
        UpdatePreview();
        
        // Don't auto-complete - user must manually check the completion box
    }

    private void UpdatePreview()
    {
        if (PdfDocument?.RasterizedImageData == null || Grid == null)
        {
            PreviewImage = null;
            return;
        }

        try
        {
            // Load the base PDF image
            using var baseStream = new MemoryStream(PdfDocument.RasterizedImageData);
            using var baseBitmap = SKBitmap.Decode(baseStream);
            if (baseBitmap == null)
            {
                PreviewImage = null;
                return;
            }

            // Create a new surface to draw on
            using var surface = SKSurface.Create(new SKImageInfo(baseBitmap.Width, baseBitmap.Height));
            var canvas = surface.Canvas;
            
            // Draw the base PDF image
            canvas.DrawBitmap(baseBitmap, 0, 0);
            
            // Draw grid lines
            var scale = baseBitmap.Width / PdfDocument.Width;
            using var gridPaint = new SKPaint
            {
                Color = SKColors.Red,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true
            };

            // Draw row lines
            foreach (var rowPos in Grid.RowPositions)
            {
                var y = (float)(rowPos * scale);
                canvas.DrawLine(0, y, baseBitmap.Width, y, gridPaint);
            }

            // Draw column lines
            foreach (var colPos in Grid.ColumnPositions)
            {
                var x = (float)(colPos * scale);
                canvas.DrawLine(x, 0, x, baseBitmap.Height, gridPaint);
            }

            // Convert to BitmapImage
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            
            using var stream = new MemoryStream(data.ToArray());
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            
            PreviewImage = bitmap;
        }
        catch
        {
            PreviewImage = null;
        }
    }

    partial void OnGridChanged(RasterGridModel? value)
    {
        UpdatePreview();
    }

    public void UpdateGridLine(bool isRow, int index, double position)
    {
        if (Grid == null) return;

        _rasterService.UpdateGridLine(Grid, isRow, index, position);
        OnPropertyChanged(nameof(Grid));
        UpdatePreview();
        
        // Notify that the grid has been updated so cutouts can be regenerated
        GridUpdated?.Invoke(Grid);
    }

    public void UpdateGridLinePosition(bool isRow, int index, double position)
    {
        if (Grid == null) return;

        // Only update the position without regenerating preview or notifying
        // This is used during dragging for better performance
        _rasterService.UpdateGridLine(Grid, isRow, index, position);
        OnPropertyChanged(nameof(Grid));
    }

    public (bool isRow, int index)? GetGridLineAtPosition(double x, double y)
    {
        if (Grid == null || PdfDocument == null || PreviewImage == null) return null;

        // Convert screen coordinates to PDF coordinates
        // Screen coordinates need to account for the offset and scale
        var imageScale = PreviewImage.PixelWidth / PdfDocument.Width;
        var totalScale = PreviewScale * imageScale;
        
        // Convert screen coordinates to PDF coordinates
        var pdfX = (x - PreviewOffsetX) / totalScale;
        var pdfY = (y - PreviewOffsetY) / totalScale;

        // Tolerance in PDF coordinates (approximately 5 screen pixels)
        var tolerance = 5.0 / totalScale;

        // Check row lines
        for (int i = 0; i < Grid.RowPositions.Count; i++)
        {
            var rowPos = Grid.RowPositions[i];
            if (Math.Abs(pdfY - rowPos) <= tolerance)
            {
                return (true, i);
            }
        }

        // Check column lines
        for (int i = 0; i < Grid.ColumnPositions.Count; i++)
        {
            var colPos = Grid.ColumnPositions[i];
            if (Math.Abs(pdfX - colPos) <= tolerance)
            {
                return (false, i);
            }
        }

        return null;
    }

    public void UpdateGridLineFromScreenPosition(bool isRow, int index, double screenX, double screenY)
    {
        if (Grid == null || PdfDocument == null || PreviewImage == null) return;

        var imageScale = PreviewImage.PixelWidth / PdfDocument.Width;
        var totalScale = PreviewScale * imageScale;
        
        double position;
        if (isRow)
        {
            var pdfY = (screenY - PreviewOffsetY) / totalScale;
            position = Math.Clamp(pdfY, 0, Grid.CanvasHeight);
        }
        else
        {
            var pdfX = (screenX - PreviewOffsetX) / totalScale;
            position = Math.Clamp(pdfX, 0, Grid.CanvasWidth);
        }

        UpdateGridLine(isRow, index, position);
    }

    [RelayCommand]
    private void ResetGrid()
    {
        CreateGrid();
    }

    partial void OnRowsChanged(int value)
    {
        // Don't auto-create grid - wait for user to press "Create Grid" button
        // This improves slider dragging performance
    }

    partial void OnColumnsChanged(int value)
    {
        // Don't auto-create grid - wait for user to press "Create Grid" button
        // This improves slider dragging performance
    }

    partial void OnPdfDocumentChanged(PdfDocumentModel? value)
    {
        if (value != null && IsEnabled)
        {
            CreateGrid();
        }
    }
}

