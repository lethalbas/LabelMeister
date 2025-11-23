using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;

namespace LabelMeister.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly IExportService _exportService;

    [ObservableProperty]
    private StripModel? _strip;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private string _statusMessage = "Ready to export";

    [ObservableProperty]
    private bool _isExporting = false;

    [ObservableProperty]
    private string _fileName = "labels.pdf";

    [ObservableProperty]
    private bool _isEnabled = false;

    public List<PlacementModel> Placements { get; set; } = new();
    public List<CutoutRegionModel> Cutouts { get; set; } = new();
    public ExportSettingsModel ExportSettings { get; set; } = new();

    public ExportViewModel(IExportService exportService)
    {
        _exportService = exportService;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    public void SetStrip(StripModel? strip)
    {
        Strip = strip;
        GeneratePreview();
    }

    public void SetPlacements(List<PlacementModel> placements)
    {
        Placements = placements;
        GeneratePreview();
    }

    public void SetCutouts(List<CutoutRegionModel> cutouts)
    {
        Cutouts = cutouts;
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        if (Strip == null || Placements.Count == 0 || Cutouts.Count == 0)
        {
            PreviewImage = null;
            return;
        }

        try
        {
            var previewData = _exportService.GeneratePreview(Placements, Cutouts, Strip);
            if (previewData != null && previewData.Length > 0)
            {
                // Validate the PNG data first
                using (var validationStream = new MemoryStream(previewData))
                {
                    validationStream.Position = 0;
                    var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(
                        validationStream, 
                        BitmapCreateOptions.None, 
                        BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count == 0 || decoder.Frames[0] == null)
                    {
                        PreviewImage = null;
                        return;
                    }
                }
                
                // Create BitmapImage from validated data
                var imageData = new byte[previewData.Length];
                Array.Copy(previewData, imageData, previewData.Length);
                
                var bitmap = new BitmapImage();
                var stream = new MemoryStream(imageData);
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                PreviewImage = bitmap;
            }
            else
            {
                PreviewImage = null;
            }
        }
        catch
        {
            PreviewImage = null;
        }
    }

    [RelayCommand]
    private void GeneratePreviewCommand()
    {
        GeneratePreview();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (Strip == null || Placements.Count == 0)
        {
            StatusMessage = "No placements to export";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = FileName,
            Title = "Save PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            IsExporting = true;
            StatusMessage = "Exporting PDF...";

            try
            {
                ExportSettings.OutputPath = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                ExportSettings.FileName = Path.GetFileName(dialog.FileName);

                var success = await _exportService.ExportAsync(Placements, Cutouts, Strip, ExportSettings);
                
                if (success)
                {
                    StatusMessage = $"PDF exported successfully: {ExportSettings.FileName}";
                }
                else
                {
                    StatusMessage = "Failed to export PDF";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting PDF: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
            }
        }
    }
}

