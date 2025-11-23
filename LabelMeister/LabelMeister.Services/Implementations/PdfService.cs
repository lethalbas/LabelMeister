using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;
using PdfiumDocument = PdfiumViewer.PdfDocument;

namespace LabelMeister.Services.Implementations;

public class PdfService : IPdfService
{
    private readonly IPdfiumNativeService _pdfiumNativeService;

    public PdfService(IPdfiumNativeService pdfiumNativeService)
    {
        _pdfiumNativeService = pdfiumNativeService;
    }

    public async Task<PdfDocumentModel?> LoadPdfAsync(string filePath)
    {
        try
        {
            using var pdfDocument = PdfPigDocument.Open(filePath);
            
            if (pdfDocument.NumberOfPages == 0)
                return null;

            var page = pdfDocument.GetPage(1);
            
            // Get page dimensions from MediaBox (bounding box)
            var mediaBox = page.MediaBox;
            var bounds = mediaBox.Bounds;
            var width = bounds.Width;
            var height = bounds.Height;

            var model = new PdfDocumentModel
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                PageCount = pdfDocument.NumberOfPages,
                Width = width,
                Height = height
            };

            // Rasterize the first page
            model.RasterizedImageData = await RasterizePageAsync(model);

            return model;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> RasterizePageAsync(PdfDocumentModel document, int pageIndex = 0, double dpi = 300.0)
    {
        // Ensure PDFium DLL is initialized before using PdfiumViewer
        // This prevents DllNotFoundException
        if (!_pdfiumNativeService.InitializePdfiumDll())
        {
            // If initialization fails, try to ensure DLLs are present first
            await _pdfiumNativeService.EnsurePdfiumNativeDllsAsync();
            if (!_pdfiumNativeService.InitializePdfiumDll())
            {
                throw new DllNotFoundException($"Failed to initialize PDFium DLL: {_pdfiumNativeService.StatusMessage}");
            }
        }

        return await Task.Run(() =>
        {
            try
            {
                // Use PdfiumViewer to render PDF page directly to bitmap
                // This preserves all visual elements: images, colors, fonts, graphics
                using var pdfDoc = PdfiumDocument.Load(document.FilePath);
                
                // Ensure the document has pages
                if (pdfDoc.PageCount == 0)
                {
                    throw new InvalidOperationException("PDF document has no pages");
                }
                
                // Ensure pageIndex is valid
                if (pageIndex < 0 || pageIndex >= pdfDoc.PageCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(pageIndex), $"Page index {pageIndex} is out of range. Document has {pdfDoc.PageCount} pages.");
                }
                
                // Calculate dimensions at specified DPI
                // PDF dimensions are in points (72 DPI), convert to pixels at target DPI
                // Use dimensions from PdfPig (already loaded in document model)
                var width = (int)(document.Width * dpi / 72.0);
                var height = (int)(document.Height * dpi / 72.0);
                
                System.Diagnostics.Debug.WriteLine($"PDF dimensions - PdfPig: {document.Width}x{document.Height} points, Render size: {width}x{height} pixels at {dpi} DPI");
                
                // Ensure dimensions are valid
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException($"Invalid dimensions: {width}x{height}");
                }
                
                // Try using the Render method that returns an Image directly (more reliable)
                try
                {
                    // Render method requires float for DPI, not double
                    using var renderedImage = pdfDoc.Render(pageIndex, width, height, (float)dpi, (float)dpi, false);
                    if (renderedImage == null)
                    {
                        throw new InvalidOperationException("PdfiumViewer Render returned null");
                    }
                    
                    // Verify the rendered image isn't completely white/empty
                    var hasContent = false;
                    try
                    {
                        using var testBitmap = new Bitmap(renderedImage);
                        // Sample a few pixels to check if image is not all white
                        for (int x = 0; x < Math.Min(10, testBitmap.Width); x += Math.Max(1, testBitmap.Width / 10))
                        {
                            for (int y = 0; y < Math.Min(10, testBitmap.Height); y += Math.Max(1, testBitmap.Height / 10))
                            {
                                var pixel = testBitmap.GetPixel(x, y);
                                if (pixel.R != 255 || pixel.G != 255 || pixel.B != 255)
                                {
                                    hasContent = true;
                                    break;
                                }
                            }
                            if (hasContent) break;
                        }
                    }
                    catch
                    {
                        // If we can't check, assume it has content
                        hasContent = true;
                    }
                    
                    if (!hasContent)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Rendered PDF image appears to be completely white (dimensions: {width}x{height})");
                        // Continue anyway - might be a valid white PDF page
                    }
                    
                    // Convert to PNG bytes
                    using var ms = new MemoryStream();
                    renderedImage.Save(ms, ImageFormat.Png);
                    var result = ms.ToArray();
                    
                    // Verify we got valid image data
                    if (result == null || result.Length == 0)
                    {
                        throw new InvalidOperationException("Rendered image data is empty");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Successfully rendered PDF page: {width}x{height} pixels, {result.Length} bytes PNG data");
                    return result;
                }
                catch (Exception renderEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Direct render failed: {renderEx.Message}. Trying Graphics-based render...");
                    
                    // Fallback: Try rendering to Graphics object if direct render fails
                    using var bitmap = new Bitmap(width, height);
                    bitmap.SetResolution((float)dpi, (float)dpi);
                    
                    using var graphics = Graphics.FromImage(bitmap);
                    // Initialize bitmap to white background
                    graphics.Clear(Color.White);
                    
                    // Set high-quality rendering
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    
                    // Render the PDF page to the graphics object
                    // The bounds should match the page size
                    var bounds = new Rectangle(0, 0, width, height);
                    
                    System.Diagnostics.Debug.WriteLine($"Attempting Graphics.Render with bounds: {bounds.Width}x{bounds.Height} at DPI {dpi}");
                    
                    // Try rendering with explicit page dimensions
                    try
                    {
                        pdfDoc.Render(pageIndex, graphics, 0, 0, bounds, false);
                        System.Diagnostics.Debug.WriteLine($"Graphics.Render completed successfully");
                    }
                    catch (Exception renderError)
                    {
                        System.Diagnostics.Debug.WriteLine($"Graphics.Render failed: {renderError.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {renderError.StackTrace}");
                        throw;
                    }
                    
                    // Verify the rendered bitmap has content
                    var hasContent = false;
                    for (int x = 0; x < Math.Min(10, bitmap.Width); x += Math.Max(1, bitmap.Width / 10))
                    {
                        for (int y = 0; y < Math.Min(10, bitmap.Height); y += Math.Max(1, bitmap.Height / 10))
                        {
                            var pixel = bitmap.GetPixel(x, y);
                            if (pixel.R != 255 || pixel.G != 255 || pixel.B != 255)
                            {
                                hasContent = true;
                                break;
                            }
                        }
                        if (hasContent) break;
                    }
                    
                    if (!hasContent)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Graphics-rendered PDF image appears to be completely white (dimensions: {width}x{height})");
                    }
                    
                    // Convert to PNG bytes
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    var result = ms.ToArray();
                    
                    if (result == null || result.Length == 0)
                    {
                        throw new InvalidOperationException($"Both rendering methods failed. Direct render error: {renderEx.Message}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Successfully rendered PDF page via Graphics: {width}x{height} pixels, {result.Length} bytes PNG data");
                    return result;
                }
            }
            catch (DllNotFoundException)
            {
                // Re-throw DllNotFoundException so caller knows the issue
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging but return null
                System.Diagnostics.Debug.WriteLine($"Error rasterizing PDF page: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        });
    }

    public async Task<bool> ExportToPdfAsync(List<PlacementModel> placements, List<CutoutRegionModel> cutouts, StripModel strip, ExportSettingsModel settings)
    {
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
                foreach (var placement in placements)
                {
                    var cutout = cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
                    if (cutout?.ImageData != null)
                    {
                        try
                        {
                            // Convert image data and draw on PDF
                            using var stream = new MemoryStream(cutout.ImageData);
                            using var image = PdfSharp.Drawing.XImage.FromStream(stream);
                            
                            var state = gfx.Save();
                            gfx.TranslateTransform(
                                PdfSharp.Drawing.XUnit.FromMillimeter(placement.X),
                                PdfSharp.Drawing.XUnit.FromMillimeter(placement.Y)
                            );
                            gfx.RotateTransform((float)placement.Rotation);
                            gfx.ScaleTransform((float)placement.ScaleX, (float)placement.ScaleY);
                            
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
}

