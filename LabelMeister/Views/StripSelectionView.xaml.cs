using System.Windows;
using System.Windows.Controls;
using LabelMeister.ViewModels;
using LabelMeister.Core.Models;

namespace LabelMeister.Views;

public partial class StripSelectionView : UserControl
{
    private StripSelectionViewModel? _viewModel;

    public StripSelectionView(StripSelectionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        
        // Subscribe to preview updates
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StripSelectionViewModel.SelectedStrip) ||
                    e.PropertyName == nameof(StripSelectionViewModel.CustomWidth) ||
                    e.PropertyName == nameof(StripSelectionViewModel.CustomHeight) ||
                    e.PropertyName == nameof(StripSelectionViewModel.IsLandscape))
                {
                    UpdatePreview();
                }
            };
        }
        
        Loaded += (s, e) => UpdatePreview();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null) return;
        
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem is StripModel selectedStrip)
        {
            if (selectedStrip.Name == "Custom")
            {
                _viewModel.IsCustomStrip = true;
            }
            else
            {
                _viewModel.IsCustomStrip = false;
                _viewModel.SelectedStrip = selectedStrip;
                _viewModel.SelectStrip(selectedStrip);
            }
        }
    }

    private void ComboBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.IsEnabled)
        {
            // Open dropdown on click anywhere on the combobox
            comboBox.IsDropDownOpen = true;
            e.Handled = true;
        }
    }

    private void UpdatePreview()
    {
        if (_viewModel == null || PreviewRectangle == null) return;

        StripModel? strip = null;
        
        if (_viewModel.IsCustomStrip)
        {
            // Use custom dimensions
            strip = new StripModel
            {
                Width = _viewModel.CustomWidth,
                Height = _viewModel.CustomHeight,
                IsLandscape = _viewModel.IsLandscape
            };
        }
        else if (_viewModel.SelectedStrip != null)
        {
            strip = _viewModel.SelectedStrip;
        }

        if (strip == null)
        {
            PreviewRectangle.Width = double.NaN;
            PreviewRectangle.Height = double.NaN;
            return;
        }

        // Calculate aspect ratio and size for preview
        var width = strip.Width;
        var height = strip.Height;
        
        if (strip.IsLandscape && width < height)
        {
            // Swap if landscape but dimensions suggest portrait
            (width, height) = (height, width);
        }
        else if (!strip.IsLandscape && width > height)
        {
            // Swap if portrait but dimensions suggest landscape
            (width, height) = (height, width);
        }

        var aspectRatio = width / height;
        
        // Calculate preview size maintaining aspect ratio (max 400x400)
        double previewWidth = 400;
        double previewHeight = 400;
        
        if (aspectRatio > 1)
        {
            // Landscape: width is limiting factor
            previewHeight = previewWidth / aspectRatio;
        }
        else
        {
            // Portrait: height is limiting factor
            previewWidth = previewHeight * aspectRatio;
        }

        PreviewRectangle.Width = previewWidth;
        PreviewRectangle.Height = previewHeight;
    }
}

