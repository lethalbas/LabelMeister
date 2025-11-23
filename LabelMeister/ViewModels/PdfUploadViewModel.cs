using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;

namespace LabelMeister.ViewModels;

public partial class PdfUploadViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private string _statusMessage = "Ready to upload PDF";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isCompleted = false;

    [ObservableProperty]
    private bool _isEnabled = true;

    public event Action<PdfDocumentModel>? PdfLoaded;
    public event Action<bool>? CompletionChanged;

    public PdfUploadViewModel(IPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        CompletionChanged?.Invoke(value);
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    [RelayCommand]
    private async Task BrowsePdfAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Title = "Select a PDF file"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadPdfAsync(dialog.FileName);
        }
    }

    private async Task LoadPdfAsync(string filePath)
    {
        IsLoading = true;
        StatusMessage = "Loading PDF...";

        try
        {
            var pdf = await _pdfService.LoadPdfAsync(filePath);
            
            if (pdf == null)
            {
                StatusMessage = "Failed to load PDF";
                return;
            }

            if (pdf.PageCount > 1)
            {
                StatusMessage = "Warning: PDF has more than 1 page. Only the first page will be used.";
            }

            // Convert rasterized image to BitmapImage
            if (pdf.RasterizedImageData != null && pdf.RasterizedImageData.Length > 0)
            {
                try
                {
                    // Validate the PNG data first
                    using (var validationStream = new MemoryStream(pdf.RasterizedImageData))
                    {
                        validationStream.Position = 0;
                        var decoder = new PngBitmapDecoder(validationStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        if (decoder.Frames.Count == 0 || decoder.Frames[0] == null)
                        {
                            PreviewImage = null;
                            StatusMessage = "PDF preview: Invalid image data";
                            return;
                        }
                        
                        // Check if image appears to be blank/white
                        var frame = decoder.Frames[0];
                        if (frame != null && frame.PixelWidth > 0 && frame.PixelHeight > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"PDF preview image: {frame.PixelWidth}x{frame.PixelHeight} pixels, {pdf.RasterizedImageData.Length} bytes");
                        }
                    }
                    
                    // Create BitmapImage from validated data
                    // CacheOption.OnLoad copies all data during EndInit, so we can use a local stream
                    var imageData = new byte[pdf.RasterizedImageData.Length];
                    Array.Copy(pdf.RasterizedImageData, imageData, pdf.RasterizedImageData.Length);
                    
                    var bitmap = new BitmapImage();
                    var memoryStream = new MemoryStream(imageData);
                    bitmap.BeginInit();
                    bitmap.StreamSource = memoryStream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    PreviewImage = bitmap;
                    StatusMessage = $"PDF loaded: {pdf.FileName} ({pdf.Width:F1}x{pdf.Height:F1} points)";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error displaying PDF preview: {ex.Message}";
                    PreviewImage = null;
                    System.Diagnostics.Debug.WriteLine($"Error in PdfUploadViewModel: {ex}");
                }
            }
            else
            {
                PreviewImage = null;
                StatusMessage = "PDF loaded but no preview data available. Rasterization may have failed.";
            }

            PdfLoaded?.Invoke(pdf);
            // Don't auto-complete - user must manually check the completion box
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading PDF: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

