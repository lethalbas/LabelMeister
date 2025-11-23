using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using SkiaSharp;
using System.IO;

namespace LabelMeister.ViewModels;

public partial class PlacementViewModel : ObservableObject
{
    private readonly IPlacementService _placementService;

    [ObservableProperty]
    private List<CutoutRegionModel> _cutouts = new();

    [ObservableProperty]
    private StripModel? _strip;

    [ObservableProperty]
    private ObservableCollection<PlacementModel> _placements = new();

    [ObservableProperty]
    private PlacementModel? _selectedPlacement;

    [ObservableProperty]
    private CutoutRegionModel? _selectedCutout;

    [ObservableProperty]
    private bool _isCompleted = false;

    public bool HasSelectedPlacement => SelectedPlacement != null;

    [ObservableProperty]
    private bool _isEnabled = false;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    private Action<List<PlacementModel>>? _placementsChanged;

    public event Action<bool>? CompletionChanged;

    public PlacementViewModel(IPlacementService placementService)
    {
        _placementService = placementService;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        CompletionChanged?.Invoke(value);
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    public void SetPlacementsChangedCallback(Action<List<PlacementModel>> callback)
    {
        _placementsChanged = callback;
    }

    private void NotifyPlacementsChanged()
    {
        _placementsChanged?.Invoke(Placements.ToList());
    }

    public void SetCutouts(List<CutoutRegionModel> cutouts)
    {
        // Filter out discarded cutouts
        Cutouts = cutouts.Where(c => !c.IsDiscarded).ToList();
        // Clear existing placements that reference removed cutouts
        var validPlacements = Placements.Where(p => Cutouts.Any(c => c.Id == p.CutoutId)).ToList();
        Placements.Clear();
        foreach (var placement in validPlacements)
        {
            Placements.Add(placement);
        }
        UpdatePreview();
    }

    public void SetStrip(StripModel strip)
    {
        Strip = strip;
        OnPropertyChanged(nameof(Strip));
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (Strip == null || Cutouts.Count == 0)
        {
            PreviewImage = null;
            return;
        }

        try
        {
            // Create strip canvas
            var widthPx = (int)(Strip.Width * Strip.Dpi / 25.4);
            var heightPx = (int)(Strip.Height * Strip.Dpi / 25.4);

            var imageInfo = new SKImageInfo(widthPx, heightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Draw strip border
            using var borderPaint = new SKPaint
            {
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true
            };
            canvas.DrawRect(0, 0, widthPx, heightPx, borderPaint);

            // Draw each placement
            // Scale: pixels per mm (widthPx is in pixels, Strip.Width is in mm)
            var scale = widthPx / Strip.Width;
            foreach (var placement in Placements.OrderBy(p => p.ZIndex))
            {
                var cutout = Cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
                if (cutout?.ImageData != null)
                {
                    try
                    {
                        using var stream = new MemoryStream(cutout.ImageData);
                        using var cutoutBitmap = SKBitmap.Decode(stream);
                        if (cutoutBitmap != null)
                        {
                            canvas.Save();
                            
                            // Convert mm to pixels
                            var x = (float)(placement.X * scale);
                            var y = (float)(placement.Y * scale);
                            
                            // Calculate the actual dimensions in pixels
                            var width = (float)(cutout.Width * placement.ScaleX * scale);
                            var height = (float)(cutout.Height * placement.ScaleY * scale);
                            
                            // Scale cutout to fit region
                            var cutoutScale = (float)(cutout.Width * scale / cutoutBitmap.Width);
                            
                            canvas.Save();
                            canvas.Translate(x, y);
                            canvas.RotateDegrees((float)placement.Rotation);
                            
                            // Draw selection border if selected (after rotation, so it matches the image)
                            if (SelectedPlacement == placement)
                            {
                                using var selectionPaint = new SKPaint
                                {
                                    Color = SKColors.Blue,
                                    Style = SKPaintStyle.Stroke,
                                    StrokeWidth = 3,
                                    IsAntialias = true
                                };
                                
                                // Draw border around the rotated/scaled bounds
                                // The border should be at the scaled size, accounting for rotation
                                var borderWidth = (float)(cutout.Width * placement.ScaleX * scale);
                                var borderHeight = (float)(cutout.Height * placement.ScaleY * scale);
                                
                                // Draw border at origin (0,0) since we're already translated and rotated
                                canvas.DrawRect(0, 0, borderWidth, borderHeight, selectionPaint);
                            }
                            
                            canvas.Scale((float)placement.ScaleX, (float)placement.ScaleY);
                            canvas.Scale(cutoutScale, cutoutScale);
                            
                            canvas.DrawBitmap(cutoutBitmap, 0, 0);
                            canvas.Restore();
                        }
                    }
                    catch
                    {
                        // Skip invalid images
                        continue;
                    }
                }
            }

            // Convert to BitmapImage
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            
            using var memStream = new MemoryStream(data.ToArray());
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = memStream;
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

    partial void OnPlacementsChanged(ObservableCollection<PlacementModel> value)
    {
        UpdatePreview();
        NotifyPlacementsChanged();
    }

    partial void OnSelectedPlacementChanged(PlacementModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedPlacement));
        UpdatePreview(); // Update preview to show selection border
    }

    [ObservableProperty]
    private double _previewScale = 1.0;

    [ObservableProperty]
    private double _previewOffsetX = 0;

    [ObservableProperty]
    private double _previewOffsetY = 0;

    public PlacementModel? GetPlacementAtPosition(double x, double y)
    {
        if (Strip == null || PreviewImage == null || Placements.Count == 0) return null;

        var imageScale = PreviewImage.PixelWidth / (Strip.Width * Strip.Dpi / 25.4);
        var totalScale = PreviewScale * imageScale;
        
        // Convert screen coordinates to strip coordinates (mm)
        var stripX = (x - PreviewOffsetX) / totalScale * (25.4 / Strip.Dpi);
        var stripY = (y - PreviewOffsetY) / totalScale * (25.4 / Strip.Dpi);

        // Find placement containing this point (check in reverse order for z-index)
        foreach (var placement in Placements.OrderByDescending(p => p.ZIndex))
        {
            var cutout = Cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
            if (cutout == null) continue;

            // Get actual dimensions accounting for rotation
            var (width, height) = GetRotatedDimensions(cutout, placement);
            
            // Calculate visual top-left position based on rotation
            double visualTopLeftX = placement.X;
            double visualTopLeftY = placement.Y;
            
            if (placement.Rotation == 90)
            {
                visualTopLeftX = placement.X - height;
                visualTopLeftY = placement.Y;
            }
            else if (placement.Rotation == 180)
            {
                visualTopLeftX = placement.X - width;
                visualTopLeftY = placement.Y - height;
            }
            else if (placement.Rotation == 270)
            {
                visualTopLeftX = placement.X;
                visualTopLeftY = placement.Y - width;
            }
            
            // Check if point is within visual bounding box
            if (stripX >= visualTopLeftX && stripX <= visualTopLeftX + width &&
                stripY >= visualTopLeftY && stripY <= visualTopLeftY + height)
            {
                return placement;
            }
        }

        return null;
    }

    private (double width, double height) GetRotatedDimensions(CutoutRegionModel cutout, PlacementModel placement)
    {
        var width = cutout.Width * placement.ScaleX;
        var height = cutout.Height * placement.ScaleY;
        
        // For 90 or 270 degree rotations, swap width and height
        if (placement.Rotation == 90 || placement.Rotation == 270)
        {
            return (height, width);
        }
        
        return (width, height);
    }

    public void UpdatePlacementFromScreenPosition(PlacementModel placement, double screenX, double screenY)
    {
        if (Strip == null || PreviewImage == null) return;

        var imageScale = PreviewImage.PixelWidth / (Strip.Width * Strip.Dpi / 25.4);
        var totalScale = PreviewScale * imageScale;
        
        // Convert screen coordinates to strip coordinates (mm)
        var stripX = (screenX - PreviewOffsetX) / totalScale * (25.4 / Strip.Dpi);
        var stripY = (screenY - PreviewOffsetY) / totalScale * (25.4 / Strip.Dpi);

        UpdatePlacement(placement, stripX, stripY);
    }

    public void UpdatePlacementPositionFromScreenPosition(PlacementModel placement, double screenX, double screenY)
    {
        if (Strip == null || PreviewImage == null) return;

        var imageScale = PreviewImage.PixelWidth / (Strip.Width * Strip.Dpi / 25.4);
        var totalScale = PreviewScale * imageScale;
        
        // Convert screen coordinates to strip coordinates (mm)
        var stripX = (screenX - PreviewOffsetX) / totalScale * (25.4 / Strip.Dpi);
        var stripY = (screenY - PreviewOffsetY) / totalScale * (25.4 / Strip.Dpi);

        // Only update position without regenerating preview (performance optimization)
        UpdatePlacementPosition(placement, stripX, stripY);
    }

    [RelayCommand]
    public void PlaceCutout(CutoutRegionModel? cutout)
    {
        if (cutout == null || Strip == null) return;

        var placement = _placementService.CreatePlacement(cutout.Id, 0, 0);
        Placements.Add(placement);
        SelectedPlacement = placement;
        UpdatePreview();
        NotifyPlacementsChanged();
    }

    private double CalculateFitScale(CutoutRegionModel cutout)
    {
        if (Strip == null) return 1.0;

        // Calculate initial scale to fit strip while maintaining aspect ratio
        var stripWidth = Strip.Width;
        var stripHeight = Strip.Height;
        var cutoutWidth = cutout.Width;
        var cutoutHeight = cutout.Height;
        
        // Calculate scale to fit within strip (leave some margin)
        var margin = 5.0; // 5mm margin
        var availableWidth = stripWidth - margin * 2;
        var availableHeight = stripHeight - margin * 2;
        
        var scaleX = availableWidth / cutoutWidth;
        var scaleY = availableHeight / cutoutHeight;
        var scale = Math.Min(scaleX, scaleY); // Use smaller scale to maintain aspect ratio
        
        // Clamp scale to reasonable values
        scale = Math.Max(0.1, Math.Min(scale, 1.0)); // Between 10% and 100%
        
        return scale;
    }

    public void PlaceCutoutAt(CutoutRegionModel cutout, double x, double y)
    {
        if (Strip == null) return;

        var scale = CalculateFitScale(cutout);
        
        var placement = _placementService.CreatePlacement(cutout.Id, x, y);
        placement.ScaleX = scale;
        placement.ScaleY = scale;
        
        Placements.Add(placement);
        SelectedPlacement = placement;
        UpdatePreview();
        NotifyPlacementsChanged();
    }

    public void UpdatePlacement(PlacementModel? placement, double x, double y)
    {
        if (placement == null || Strip == null) return;

        // Clamp to strip bounds, accounting for rotation
        // The origin (x, y) is the rotation center, but bounds checking needs to use the visual bounds
        var cutout = Cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
        if (cutout != null)
        {
            var (width, height) = GetRotatedDimensions(cutout, placement);
            
            // Calculate visual top-left position based on rotation
            double visualTopLeftX = x;
            double visualTopLeftY = y;
            
            if (placement.Rotation == 90)
            {
                visualTopLeftX = x - height;
                visualTopLeftY = y;
            }
            else if (placement.Rotation == 180)
            {
                visualTopLeftX = x - width;
                visualTopLeftY = y - height;
            }
            else if (placement.Rotation == 270)
            {
                visualTopLeftX = x;
                visualTopLeftY = y - width;
            }
            
            // Clamp visual top-left to stay within bounds
            visualTopLeftX = Math.Max(0, Math.Min(visualTopLeftX, Strip.Width - width));
            visualTopLeftY = Math.Max(0, Math.Min(visualTopLeftY, Strip.Height - height));
            
            // Convert back to rotation center coordinates
            if (placement.Rotation == 90)
            {
                x = visualTopLeftX + height;
                y = visualTopLeftY;
            }
            else if (placement.Rotation == 180)
            {
                x = visualTopLeftX + width;
                y = visualTopLeftY + height;
            }
            else if (placement.Rotation == 270)
            {
                x = visualTopLeftX;
                y = visualTopLeftY + width;
            }
            else
            {
                x = visualTopLeftX;
                y = visualTopLeftY;
            }
        }

        _placementService.UpdatePlacement(placement, x, y);
        OnPropertyChanged(nameof(Placements));
        // Notify that SelectedPlacement changed to refresh bindings
        // Since PlacementModel doesn't implement INotifyPropertyChanged, we need to notify
        // the SelectedPlacement property change to refresh the text box bindings
        OnPropertyChanged(nameof(SelectedPlacement));
        UpdatePreview();
        NotifyPlacementsChanged();
    }

    public void UpdatePlacementPosition(PlacementModel? placement, double x, double y)
    {
        if (placement == null || Strip == null) return;

        // Clamp to strip bounds, accounting for rotation
        // The origin (x, y) is the rotation center, but bounds checking needs to use the visual bounds
        var cutout = Cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
        if (cutout != null)
        {
            var (width, height) = GetRotatedDimensions(cutout, placement);
            
            // Calculate visual top-left position based on rotation
            double visualTopLeftX = x;
            double visualTopLeftY = y;
            
            if (placement.Rotation == 90)
            {
                visualTopLeftX = x - height;
                visualTopLeftY = y;
            }
            else if (placement.Rotation == 180)
            {
                visualTopLeftX = x - width;
                visualTopLeftY = y - height;
            }
            else if (placement.Rotation == 270)
            {
                visualTopLeftX = x;
                visualTopLeftY = y - width;
            }
            
            // Clamp visual top-left to stay within bounds
            visualTopLeftX = Math.Max(0, Math.Min(visualTopLeftX, Strip.Width - width));
            visualTopLeftY = Math.Max(0, Math.Min(visualTopLeftY, Strip.Height - height));
            
            // Convert back to rotation center coordinates
            if (placement.Rotation == 90)
            {
                x = visualTopLeftX + height;
                y = visualTopLeftY;
            }
            else if (placement.Rotation == 180)
            {
                x = visualTopLeftX + width;
                y = visualTopLeftY + height;
            }
            else if (placement.Rotation == 270)
            {
                x = visualTopLeftX;
                y = visualTopLeftY + width;
            }
            else
            {
                x = visualTopLeftX;
                y = visualTopLeftY;
            }
        }

        // Only update the position without regenerating preview or notifying
        // This is used during dragging for better performance
        _placementService.UpdatePlacement(placement, x, y);
        OnPropertyChanged(nameof(Placements));
    }

    public void UpdatePlacementScale(PlacementModel? placement, double scaleX, double scaleY)
    {
        if (placement == null || Strip == null) return;

        // Use the service to update scale (which will clamp values)
        _placementService.ScalePlacement(placement, scaleX, scaleY);
        
        // Ensure placement stays within strip bounds after scaling, accounting for rotation
        var cutout = Cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
        if (cutout != null)
        {
            // Get rotated dimensions to calculate correct bounds
            var (width, height) = GetRotatedDimensions(cutout, placement);
            var maxX = Strip.Width - width;
            var maxY = Strip.Height - height;
            placement.X = Math.Max(0, Math.Min(placement.X, Math.Max(0, maxX)));
            placement.Y = Math.Max(0, Math.Min(placement.Y, Math.Max(0, maxY)));
        }
        
        // Force property change notifications to update the UI
        OnPropertyChanged(nameof(Placements));
        OnPropertyChanged(nameof(SelectedPlacement));
        
        // Update the preview to show the changes
        UpdatePreview();
        NotifyPlacementsChanged();
    }

    [RelayCommand]
    private void RotatePlacement(PlacementModel? placement)
    {
        if (placement == null || Strip == null) return;

        var cutout = Cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
        if (cutout == null) return;

        // Calculate the visual center position BEFORE rotation
        // The origin (X, Y) is the rotation center, and visual position depends on rotation:
        //   0°:   visual top-left = (X, Y), center = (X + width/2, Y + height/2)
        //   90°:  visual top-left = (X - height, Y), center = (X - height/2, Y + width/2)
        //   180°: visual top-left = (X - width, Y - height), center = (X - width/2, Y - height/2)
        //   270°: visual top-left = (X, Y - width), center = (X + height/2, Y - width/2)
        var (oldWidth, oldHeight) = GetRotatedDimensions(cutout, placement);
        var oldRotation = placement.Rotation;
        
        // Calculate the visual center position before rotation
        double centerX = placement.X;
        double centerY = placement.Y;
        
        if (oldRotation == 0)
        {
            centerX = placement.X + oldWidth / 2;
            centerY = placement.Y + oldHeight / 2;
        }
        else if (oldRotation == 90)
        {
            centerX = placement.X - oldHeight / 2;
            centerY = placement.Y + oldWidth / 2;
        }
        else if (oldRotation == 180)
        {
            centerX = placement.X - oldWidth / 2;
            centerY = placement.Y - oldHeight / 2;
        }
        else if (oldRotation == 270)
        {
            centerX = placement.X + oldHeight / 2;
            centerY = placement.Y - oldWidth / 2;
        }

        var newRotation = (placement.Rotation + 90) % 360;
        _placementService.RotatePlacement(placement, newRotation);
        
        // Resize to fit within strip while maintaining aspect ratio
        var stripWidth = Strip.Width;
        var stripHeight = Strip.Height;
        var cutoutWidth = cutout.Width;
        var cutoutHeight = cutout.Height;
        
        // For 90/270 degree rotations, swap dimensions for fitting calculation
        if (newRotation == 90 || newRotation == 270)
        {
            (cutoutWidth, cutoutHeight) = (cutoutHeight, cutoutWidth);
        }
        
        // Calculate scale to fit within strip (leave some margin)
        var margin = 5.0; // 5mm margin
        var availableWidth = stripWidth - margin * 2;
        var availableHeight = stripHeight - margin * 2;
        
        var scaleX = availableWidth / cutoutWidth;
        var scaleY = availableHeight / cutoutHeight;
        var scale = Math.Min(scaleX, scaleY); // Use smaller scale to maintain aspect ratio
        
        // Clamp scale to reasonable values
        scale = Math.Max(0.1, Math.Min(scale, 1.0)); // Between 10% and 100%
        
        placement.ScaleX = scale;
        placement.ScaleY = scale;
        
        // Calculate new dimensions after rotation and scaling
        var (newWidth, newHeight) = GetRotatedDimensions(cutout, placement);
        
        // Calculate what the new origin (X, Y) should be to keep visual center at the same place
        // After rotation, the relationship between origin and visual center changes:
        double newX = centerX;
        double newY = centerY;
        
        if (newRotation == 0)
        {
            // Center = (X + width/2, Y + height/2), so: X = centerX - width/2, Y = centerY - height/2
            newX = centerX - newWidth / 2;
            newY = centerY - newHeight / 2;
        }
        else if (newRotation == 90)
        {
            // Center = (X - height/2, Y + width/2), so: X = centerX + height/2, Y = centerY - width/2
            newX = centerX + newHeight / 2;
            newY = centerY - newWidth / 2;
        }
        else if (newRotation == 180)
        {
            // Center = (X - width/2, Y - height/2), so: X = centerX + width/2, Y = centerY + height/2
            newX = centerX + newWidth / 2;
            newY = centerY + newHeight / 2;
        }
        else if (newRotation == 270)
        {
            // Center = (X + height/2, Y - width/2), so: X = centerX - height/2, Y = centerY + width/2
            newX = centerX - newHeight / 2;
            newY = centerY + newWidth / 2;
        }
        
        // Ensure placement stays within strip bounds using the proper bounds checking
        // Use UpdatePlacement to ensure proper bounds clamping
        UpdatePlacement(placement, newX, newY);
        
        // Force property change notifications to update the UI
        OnPropertyChanged(nameof(Placements));
        OnPropertyChanged(nameof(SelectedPlacement));
        UpdatePreview();
        NotifyPlacementsChanged();
    }

    [RelayCommand]
    private void RemovePlacement(PlacementModel? placement)
    {
        if (placement == null) return;

        Placements.Remove(placement);
        if (SelectedPlacement == placement)
        {
            SelectedPlacement = null;
        }
        UpdatePreview();
        NotifyPlacementsChanged();
    }
}

