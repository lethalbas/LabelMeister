using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using SkiaSharp;
using System.IO;

namespace LabelMeister.ViewModels;

public partial class MoveCombineViewModel : ObservableObject
{
    private readonly ICutoutService _cutoutService;
    private readonly IRasterService _rasterService;

    [ObservableProperty]
    private RasterGridModel? _grid;

    [ObservableProperty]
    private ObservableCollection<CutoutRegionModel> _cutoutRegions = new();

    [ObservableProperty]
    private ObservableCollection<CutoutRegionModel> _selectedRegions = new();

    [ObservableProperty]
    private bool _isCompleted = false;

    [ObservableProperty]
    private bool _isEnabled = false;

    [ObservableProperty]
    private PdfDocumentModel? _pdfDocument;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private double _previewScale = 1.0;

    [ObservableProperty]
    private double _previewOffsetX = 0;

    [ObservableProperty]
    private double _previewOffsetY = 0;

    public event Action<List<CutoutRegionModel>>? CutoutsChanged;
    public event Action<bool>? CompletionChanged;

    public MoveCombineViewModel(ICutoutService cutoutService, IRasterService rasterService)
    {
        _cutoutService = cutoutService;
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

    public void SetGrid(RasterGridModel grid)
    {
        // Only update if the grid reference actually changed, or if it's null
        if (Grid != grid)
        {
            Grid = grid;
            if (PdfDocument != null && grid != null)
            {
                GenerateCutouts();
                UpdatePreview();
            }
        }
        else if (grid != null && PdfDocument != null)
        {
            // Grid reference is the same, but positions may have changed
            // Regenerate cutouts to reflect the updated grid line positions
            GenerateCutouts();
            UpdatePreview();
        }
    }

    public void SetPdfDocument(PdfDocumentModel pdf)
    {
        PdfDocument = pdf;
        UpdatePreview();
    }

    [RelayCommand]
    public void GenerateCutouts()
    {
        if (Grid == null || PdfDocument?.RasterizedImageData == null) return;

        var regions = _rasterService.GenerateCutoutRegions(Grid);
        CutoutRegions.Clear();
        
        // Extract image data for each region
        try
        {
            using var baseStream = new MemoryStream(PdfDocument.RasterizedImageData);
            using var baseBitmap = SKBitmap.Decode(baseStream);
            if (baseBitmap != null)
            {
                var scale = baseBitmap.Width / PdfDocument.Width;
                foreach (var region in regions)
                {
                    // Crop the region from the base image
                    var x = (int)(region.X * scale);
                    var y = (int)(region.Y * scale);
                    var width = (int)(region.Width * scale);
                    var height = (int)(region.Height * scale);
                    
                    // Ensure bounds are within image
                    x = Math.Max(0, Math.Min(x, baseBitmap.Width - 1));
                    y = Math.Max(0, Math.Min(y, baseBitmap.Height - 1));
                    width = Math.Min(width, baseBitmap.Width - x);
                    height = Math.Min(height, baseBitmap.Height - y);
                    
                    if (width > 0 && height > 0)
                    {
                        using var croppedBitmap = new SKBitmap(width, height);
                        if (baseBitmap.ExtractSubset(croppedBitmap, new SKRectI(x, y, x + width, y + height)))
                        {
                            using var image = SKImage.FromBitmap(croppedBitmap);
                            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                            region.ImageData = data.ToArray();
                        }
                    }
                    
                    CutoutRegions.Add(region);
                }
            }
            else
            {
                // If bitmap decode fails, just add regions without image data
                foreach (var region in regions)
                {
                    CutoutRegions.Add(region);
                }
            }
        }
        catch
        {
            // If extraction fails, just add regions without image data
            foreach (var region in regions)
            {
                CutoutRegions.Add(region);
            }
        }

        CutoutsChanged?.Invoke(CutoutRegions.ToList());
        UpdatePreview();
    }

    [RelayCommand]
    private void CombineSelected()
    {
        if (SelectedRegions.Count < 2 || !IsEnabled) return;

        try
        {
            var combined = _cutoutService.CombineRegions(SelectedRegions.ToList());
            
            // Combine image data from selected regions
            if (PdfDocument?.RasterizedImageData != null)
            {
                try
                {
                    using var baseStream = new MemoryStream(PdfDocument.RasterizedImageData);
                    using var baseBitmap = SKBitmap.Decode(baseStream);
                    if (baseBitmap != null)
                    {
                        var scale = baseBitmap.Width / PdfDocument.Width;
                        var x = (int)(combined.X * scale);
                        var y = (int)(combined.Y * scale);
                        var width = (int)(combined.Width * scale);
                        var height = (int)(combined.Height * scale);
                        
                        x = Math.Max(0, Math.Min(x, baseBitmap.Width - 1));
                        y = Math.Max(0, Math.Min(y, baseBitmap.Height - 1));
                        width = Math.Min(width, baseBitmap.Width - x);
                        height = Math.Min(height, baseBitmap.Height - y);
                        
                        if (width > 0 && height > 0)
                        {
                            using var croppedBitmap = new SKBitmap(width, height);
                            if (baseBitmap.ExtractSubset(croppedBitmap, new SKRectI(x, y, x + width, y + height)))
                            {
                                using var image = SKImage.FromBitmap(croppedBitmap);
                                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                                combined.ImageData = data.ToArray();
                            }
                        }
                    }
                }
                catch
                {
                    // If extraction fails, continue without image data
                }
            }
            
            // Remove old regions
            foreach (var region in SelectedRegions)
            {
                CutoutRegions.Remove(region);
            }

            // Add combined region
            CutoutRegions.Add(combined);
            SelectedRegions.Clear();
            SelectedRegions.Add(combined);

            CutoutsChanged?.Invoke(CutoutRegions.ToList());
            UpdatePreview();
        }
        catch (ArgumentException ex)
        {
            // Show error message to user - could use MessageBox or a notification system
            System.Windows.MessageBox.Show(
                ex.Message,
                "Cannot Combine Regions",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void DiscardSelected()
    {
        foreach (var region in SelectedRegions.ToList())
        {
            _cutoutService.DiscardRegion(region);
        }
        SelectedRegions.Clear();
        CutoutsChanged?.Invoke(CutoutRegions.ToList());
        UpdatePreview();
    }

    [RelayCommand]
    private void RestoreSelected()
    {
        foreach (var region in SelectedRegions.ToList())
        {
            if (region.IsDiscarded)
            {
                _cutoutService.RestoreRegion(region);
            }
        }
        SelectedRegions.Clear();
        CutoutsChanged?.Invoke(CutoutRegions.ToList());
        UpdatePreview();
    }

    [RelayCommand]
    public void SelectRegion(CutoutRegionModel? region)
    {
        if (region == null || !IsEnabled) return;

        if (SelectedRegions.Contains(region))
        {
            SelectedRegions.Remove(region);
        }
        else
        {
            SelectedRegions.Add(region);
        }
        UpdatePreview();
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
            
            // Draw cutout regions
            var scale = baseBitmap.Width / PdfDocument.Width;

            // Draw filled overlays with 20% opacity (80% of PDF shines through)
            // Filter out discarded cutouts
            foreach (var region in CutoutRegions.Where(r => !r.IsDiscarded))
            {
                var x = (float)(region.X * scale);
                var y = (float)(region.Y * scale);
                var width = (float)(region.Width * scale);
                var height = (float)(region.Height * scale);
                
                SKColor overlayColor;
                
                // Determine color based on state (controlled by button actions)
                // Priority: Yellow (selected) > Red (discarded) > Green (combined) > White (unedited)
                // Yellow = currently selected (temporary state)
                // Red = discarded (after clicking "Discard Selected")
                // Green = combined (after clicking "Combine Selected")
                // White = unedited (default state)
                if (SelectedRegions.Contains(region))
                {
                    // Yellow for selected (highest priority - temporary state)
                    overlayColor = new SKColor(255, 255, 0, 51); // Yellow with 20% opacity (255 * 0.2 = 51)
                }
                else if (region.IsDiscarded)
                {
                    // Red for discarded
                    overlayColor = new SKColor(255, 0, 0, 51); // Red with 20% opacity
                }
                else if (region.CombinedCellIds != null && region.CombinedCellIds.Count > 1)
                {
                    // Green for combined
                    overlayColor = new SKColor(0, 255, 0, 51); // Green with 20% opacity
                }
                else
                {
                    // White for unedited (default state)
                    overlayColor = new SKColor(255, 255, 255, 51); // White with 20% opacity
                }
                
                using var overlayPaint = new SKPaint
                {
                    Color = overlayColor,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                
                // Draw filled overlay
                canvas.DrawRect(x, y, width, height, overlayPaint);
                
                // Draw border for better visibility
                using var borderPaint = new SKPaint
                {
                    Color = overlayColor.WithAlpha(255), // Full opacity border
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = SelectedRegions.Contains(region) ? 3 : 2,
                    IsAntialias = true
                };
                
                canvas.DrawRect(x, y, width, height, borderPaint);
                
                // Draw cutout number (only for non-discarded regions)
                // Make numbers larger and 50% transparent
                using var textPaint = new SKPaint
                {
                    Color = new SKColor(0, 0, 0, 128), // Black with 50% transparency (128 = 50% of 255)
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                    TextSize = 40, // Much larger text size
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                };
                
                var text = $"#{region.Id}";
                var textBounds = new SKRect();
                textPaint.MeasureText(text, ref textBounds);
                
                // Draw text with background for visibility (also 50% transparent)
                var textX = x + 10;
                var textY = y + textBounds.Height + 10;
                
                using var textBgPaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, 128), // White background with 50% transparency
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                
                canvas.DrawRect(textX - 4, textY - textBounds.Height - 4, 
                               textBounds.Width + 8, textBounds.Height + 8, textBgPaint);
                canvas.DrawText(text, textX, textY, textPaint);
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

    partial void OnCutoutRegionsChanged(ObservableCollection<CutoutRegionModel> value)
    {
        UpdatePreview();
    }

    partial void OnSelectedRegionsChanged(ObservableCollection<CutoutRegionModel> value)
    {
        UpdatePreview();
    }

    public CutoutRegionModel? GetRegionAtPosition(double x, double y)
    {
        if (PdfDocument == null || PreviewImage == null) return null;

        var imageScale = PreviewImage.PixelWidth / PdfDocument.Width;
        var totalScale = PreviewScale * imageScale;
        
        // Convert screen coordinates to PDF coordinates
        var pdfX = (x - PreviewOffsetX) / totalScale;
        var pdfY = (y - PreviewOffsetY) / totalScale;

        // Find the region containing this point (exclude discarded)
        foreach (var region in CutoutRegions.Where(r => !r.IsDiscarded))
        {
            if (pdfX >= region.X && pdfX <= region.X + region.Width &&
                pdfY >= region.Y && pdfY <= region.Y + region.Height)
            {
                return region;
            }
        }

        return null;
    }
}

