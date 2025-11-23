using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using SkiaSharp;
using System.IO;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace LabelMeister.Services.Implementations;

public class ExportService : IExportService
{
    public ExportService()
    {
    }

    public async Task<bool> ExportAsync(List<PlacementModel> placements, List<CutoutRegionModel> cutouts, StripModel strip, ExportSettingsModel settings)
    {
        // Use PdfSharp directly for export
        return await Task.Run(() =>
        {
            try
            {
                using var document = new PdfSharp.Pdf.PdfDocument();
                var page = document.AddPage();
                page.Width = PdfSharp.Drawing.XUnit.FromMillimeter(strip.Width);
                page.Height = PdfSharp.Drawing.XUnit.FromMillimeter(strip.Height);

                using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

                // Draw each placement
                foreach (var placement in placements.OrderBy(p => p.ZIndex))
                {
                    var cutout = cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
                    if (cutout?.ImageData != null)
                    {
                        try
                        {
                            using var stream = new MemoryStream(cutout.ImageData);
                            using var image = PdfSharp.Drawing.XImage.FromStream(stream);

                            var state = gfx.Save();
                            
                            // Translate to placement position (in mm, converted to points)
                            gfx.TranslateTransform(
                                PdfSharp.Drawing.XUnit.FromMillimeter(placement.X),
                                PdfSharp.Drawing.XUnit.FromMillimeter(placement.Y)
                            );
                            
                            // Rotate
                            gfx.RotateTransform((float)placement.Rotation);
                            
                            // Apply placement scale
                            gfx.ScaleTransform((float)placement.ScaleX, (float)placement.ScaleY);
                            
                            // Scale image to match cutout dimensions (1mm = 1 point in PDF)
                            // Image width/height are in pixels, cutout width/height are in mm
                            // We need to scale so that the image fits the cutout region size
                            var imageWidthMm = cutout.Width; // Cutout width in mm
                            var imageHeightMm = cutout.Height; // Cutout height in mm
                            
                            // Scale from image pixels to mm (points in PDF)
                            // XImage dimensions are in points (1/72 inch), but we work in mm
                            // 1 mm = 72/25.4 points
                            var pointsPerMm = 72.0 / 25.4;
                            var targetWidthPoints = imageWidthMm * pointsPerMm;
                            var targetHeightPoints = imageHeightMm * pointsPerMm;
                            
                            // Calculate scale factor to fit image to cutout size
                            var scaleX = targetWidthPoints / image.PixelWidth;
                            var scaleY = targetHeightPoints / image.PixelHeight;
                            
                            // Apply scale to fit the cutout region
                            gfx.ScaleTransform((float)scaleX, (float)scaleY);

                            gfx.DrawImage(image, 0, 0);
                            gfx.Restore(state);
                        }
                        catch
                        {
                            // Skip invalid images
                            continue;
                        }
                    }
                }

                // Save PDF
                var outputPath = Path.Combine(settings.OutputPath, settings.FileName);
                document.Save(outputPath);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public byte[]? GeneratePreview(List<PlacementModel> placements, List<CutoutRegionModel> cutouts, StripModel strip)
    {
        try
        {
            // Create strip canvas at actual size
            var widthPx = (int)(strip.Width * strip.Dpi / 25.4);
            var heightPx = (int)(strip.Height * strip.Dpi / 25.4);

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
            var scale = widthPx / strip.Width;
            foreach (var placement in placements.OrderBy(p => p.ZIndex))
            {
                var cutout = cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
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
                            
                            // Scale cutout to fit region
                            var cutoutScale = (float)(cutout.Width * scale / cutoutBitmap.Width);
                            
                            canvas.Save();
                            canvas.Translate(x, y);
                            canvas.RotateDegrees((float)placement.Rotation);
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

            using var snapshot = surface.Snapshot();
            using var data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }
}

