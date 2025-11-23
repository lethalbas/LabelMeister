using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace LabelMeister.ViewModels;

public partial class StripSelectionViewModel : ObservableObject
{
    private readonly IStripService _stripService;

    [ObservableProperty]
    private ObservableCollection<StripModel> _availableStrips = new();

    [ObservableProperty]
    private StripModel? _selectedStrip;

    [ObservableProperty]
    private bool _isCustomStrip;

    [ObservableProperty]
    private double _customWidth = 100;

    [ObservableProperty]
    private double _customHeight = 150;

    [ObservableProperty]
    private bool _isLandscape = false;

    [ObservableProperty]
    private bool _isCompleted = false;

    [ObservableProperty]
    private bool _isEnabled = false;

    [ObservableProperty]
    private string _previewText = "Select or create a strip to preview";

    public event Action<StripModel>? StripSelected;
    public event Action<bool>? CompletionChanged;

    public StripSelectionViewModel(IStripService stripService)
    {
        _stripService = stripService;
        LoadPredefinedStrips();
    }

    partial void OnIsCompletedChanged(bool value)
    {
        CompletionChanged?.Invoke(value);
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    partial void OnSelectedStripChanged(StripModel? value)
    {
        if (value != null && !IsCustomStrip)
        {
            UpdatePreviewText(value);
            StripSelected?.Invoke(value);
        }
    }

    partial void OnIsCustomStripChanged(bool value)
    {
        if (value)
        {
            // When custom is enabled, show preview based on custom dimensions
            UpdateCustomPreview();
            // Update placement tab with custom strip
            UpdateCustomStripForPlacement();
            // Set SelectedStrip to Custom option if available
            var customOption = AvailableStrips.FirstOrDefault(s => s.Name == "Custom");
            if (customOption != null)
            {
                SelectedStrip = customOption;
            }
        }
        else if (!value && SelectedStrip != null)
        {
            UpdatePreviewText(SelectedStrip);
        }
    }

    partial void OnCustomWidthChanged(double value)
    {
        if (IsCustomStrip)
        {
            UpdateCustomPreview();
            // Update placement tab when custom dimensions change
            UpdateCustomStripForPlacement();
        }
    }

    partial void OnCustomHeightChanged(double value)
    {
        if (IsCustomStrip)
        {
            UpdateCustomPreview();
            // Update placement tab when custom dimensions change
            UpdateCustomStripForPlacement();
        }
    }

    partial void OnIsLandscapeChanged(bool value)
    {
        // Always update preview when landscape changes
        if (IsCustomStrip)
        {
            UpdateCustomPreview();
            // Update placement tab when landscape changes in custom mode
            UpdateCustomStripForPlacement();
        }
    }

    private void UpdateCustomStripForPlacement()
    {
        // Only update if enabled and custom strip mode is active
        if (!IsEnabled) return;
        if (!IsCustomStrip) return;
        
        var customStrip = _stripService.CreateCustomStrip(CustomWidth, CustomHeight, IsLandscape);
        SelectedStrip = customStrip;
        StripSelected?.Invoke(customStrip);
    }

    private void UpdatePreviewText(StripModel strip)
    {
        var width = strip.Width;
        var height = strip.Height;
        
        if (strip.IsLandscape && width < height)
        {
            (width, height) = (height, width);
        }
        else if (!strip.IsLandscape && width > height)
        {
            (width, height) = (height, width);
        }

        PreviewText = $"{width:F1}mm × {height:F1}mm\n{(strip.IsLandscape ? "Landscape" : "Portrait")}";
    }

    private void UpdateCustomPreview()
    {
        var customStrip = new StripModel
        {
            Width = CustomWidth,
            Height = CustomHeight,
            IsLandscape = IsLandscape
        };
        UpdatePreviewText(customStrip);
    }

    private void LoadPredefinedStrips()
    {
        var strips = _stripService.GetPredefinedStrips();
        AvailableStrips.Clear();
        foreach (var strip in strips)
        {
            AvailableStrips.Add(strip);
        }
        // Add Custom option
        AvailableStrips.Add(new StripModel { Name = "Custom", Width = 0, Height = 0 });
        // Don't auto-select - user must manually select
    }

    [RelayCommand]
    public void SelectStrip(StripModel? strip)
    {
        if (strip == null || !IsEnabled) return;

        if (strip.Name == "Custom")
        {
            IsCustomStrip = true;
            // Update placement tab with custom strip
            UpdateCustomStripForPlacement();
        }
        else
        {
            IsCustomStrip = false;
            SelectedStrip = strip;
            UpdatePreviewText(strip);
            StripSelected?.Invoke(strip);
        }
    }
}

