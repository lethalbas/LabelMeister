using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LabelMeister.ViewModels;

namespace LabelMeister.Views;

public partial class MoveCombineView : UserControl
{
    private MoveCombineViewModel? _viewModel;

    public MoveCombineView(MoveCombineViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        
        // Subscribe to image loaded event to update canvas size
        PreviewImageControl.Loaded += PreviewImageControl_Loaded;
        
        // Update canvas when preview image changes
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MoveCombineViewModel.PreviewImage))
                {
                    UpdateCanvasSize();
                }
            };
        }
        
        // Update when image or border size changes
        PreviewImageControl.SizeChanged += (s, e) =>
        {
            UpdateCanvasSize();
        };
        
        PreviewBorder.SizeChanged += (s, e) =>
        {
            UpdateCanvasSize();
        };
    }

    private void PreviewImageControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        if (PreviewImageControl.Source is System.Windows.Media.Imaging.BitmapImage bitmap && _viewModel != null)
        {
            // Match canvas size to the border container (accounting for padding)
            var borderPadding = PreviewBorder.Padding;
            var availableWidth = PreviewBorder.ActualWidth - borderPadding.Left - borderPadding.Right;
            var availableHeight = PreviewBorder.ActualHeight - borderPadding.Top - borderPadding.Bottom;
            
            RegionCanvas.Width = availableWidth > 0 ? availableWidth : PreviewBorder.ActualWidth;
            RegionCanvas.Height = availableHeight > 0 ? availableHeight : PreviewBorder.ActualHeight;
            
            // Calculate scale and offset based on actual displayed image size
            if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
            {
                // Get the actual displayed size of the image (after Stretch="Uniform")
                var imageWidth = PreviewImageControl.ActualWidth > 0 ? PreviewImageControl.ActualWidth : bitmap.PixelWidth;
                var imageHeight = PreviewImageControl.ActualHeight > 0 ? PreviewImageControl.ActualHeight : bitmap.PixelHeight;
                
                // Calculate uniform scale (Stretch="Uniform" with MaxWidth/MaxHeight constraints)
                var maxWidth = Math.Min(800.0, availableWidth);
                var maxHeight = Math.Min(600.0, availableHeight);
                var scaleX = maxWidth / bitmap.PixelWidth;
                var scaleY = maxHeight / bitmap.PixelHeight;
                _viewModel.PreviewScale = Math.Min(scaleX, scaleY);
                
                // Calculate actual displayed image dimensions
                var displayedWidth = bitmap.PixelWidth * _viewModel.PreviewScale;
                var displayedHeight = bitmap.PixelHeight * _viewModel.PreviewScale;
                
                // Calculate offset to center the image within the canvas
                // Account for border padding
                _viewModel.PreviewOffsetX = borderPadding.Left + (RegionCanvas.Width - displayedWidth) / 2;
                _viewModel.PreviewOffsetY = borderPadding.Top + (RegionCanvas.Height - displayedHeight) / 2;
            }
        }
    }

    private void RegionCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null || !_viewModel.IsEnabled) return;

        var pos = e.GetPosition(RegionCanvas);
        var region = _viewModel.GetRegionAtPosition(pos.X, pos.Y);
        
        if (region != null)
        {
            _viewModel.SelectRegion(region);
            e.Handled = true;
        }
    }
}

