using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using LabelMeister.ViewModels;
using LabelMeister.Core.Models;

namespace LabelMeister.Views;

public partial class PlacementView : UserControl
{
    private PlacementViewModel? _viewModel;
    private CutoutRegionModel? _draggedCutout;
    private PlacementModel? _draggedPlacement;
    private bool _isDragging = false;
    private bool _isResizing = false;
    private Rectangle? _dragOutline;
    private double _dragOffsetX = 0; // Offset from mouse to placement center when drag starts
    private double _dragOffsetY = 0;

    public PlacementView(PlacementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        
        // Subscribe to preview image changes to update canvas size
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlacementViewModel.PreviewImage))
                {
                    UpdateCanvasSize();
                }
            };
        }
        
        PreviewImageControl.Loaded += PreviewImageControl_Loaded;
        PreviewImageControl.SizeChanged += (s, e) => UpdateCanvasSize();
        PreviewBorder.SizeChanged += (s, e) => UpdateCanvasSize();
    }

    private void PreviewImageControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        if (PreviewImageControl.Source is System.Windows.Media.Imaging.BitmapImage bitmap && _viewModel != null)
        {
            var borderPadding = PreviewBorder.Padding;
            var availableWidth = PreviewBorder.ActualWidth - borderPadding.Left - borderPadding.Right;
            var availableHeight = PreviewBorder.ActualHeight - borderPadding.Top - borderPadding.Bottom;
            
            PlacementCanvas.Width = availableWidth > 0 ? availableWidth : PreviewBorder.ActualWidth;
            PlacementCanvas.Height = availableHeight > 0 ? availableHeight : PreviewBorder.ActualHeight;
            
            if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
            {
                var imageWidth = PreviewImageControl.ActualWidth > 0 ? PreviewImageControl.ActualWidth : bitmap.PixelWidth;
                var imageHeight = PreviewImageControl.ActualHeight > 0 ? PreviewImageControl.ActualHeight : bitmap.PixelHeight;
                
                var maxWidth = Math.Min(800.0, availableWidth);
                var maxHeight = Math.Min(600.0, availableHeight);
                var scaleX = maxWidth / bitmap.PixelWidth;
                var scaleY = maxHeight / bitmap.PixelHeight;
                _viewModel.PreviewScale = Math.Min(scaleX, scaleY);
                
                var displayedWidth = bitmap.PixelWidth * _viewModel.PreviewScale;
                var displayedHeight = bitmap.PixelHeight * _viewModel.PreviewScale;
                
                _viewModel.PreviewOffsetX = borderPadding.Left + (PlacementCanvas.Width - displayedWidth) / 2;
                _viewModel.PreviewOffsetY = borderPadding.Top + (PlacementCanvas.Height - displayedHeight) / 2;
            }
        }
    }

    private void CutoutsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null || !_viewModel.IsEnabled) return;

        var listBox = sender as ListBox;
        var hitTestResult = System.Windows.Media.VisualTreeHelper.HitTest(listBox, e.GetPosition(listBox));
        
        if (hitTestResult?.VisualHit != null)
        {
            var item = GetAncestorOfType<ListBoxItem>(hitTestResult.VisualHit);
            if (item?.Content is CutoutRegionModel cutout)
            {
                _draggedCutout = cutout;
                listBox.SelectedItem = cutout;
                
                // Start drag operation
                var dragData = new DataObject("CutoutRegion", cutout);
                var result = DragDrop.DoDragDrop(listBox, dragData, DragDropEffects.Copy);
                
                _draggedCutout = null;
                e.Handled = true;
            }
        }
    }

    private static T? GetAncestorOfType<T>(DependencyObject element) where T : DependencyObject
    {
        while (element != null && !(element is T))
        {
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return element as T;
    }

    private void PlacementCanvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("CutoutRegion"))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void PlacementCanvas_Drop(object sender, DragEventArgs e)
    {
        if (_viewModel == null || !_viewModel.IsEnabled) return;

        if (e.Data.GetData("CutoutRegion") is CutoutRegionModel cutout)
        {
            var pos = e.GetPosition(PlacementCanvas);
            
            // Convert screen coordinates to strip coordinates
            if (_viewModel.Strip != null && _viewModel.PreviewImage != null)
            {
                var imageScale = _viewModel.PreviewImage.PixelWidth / (_viewModel.Strip.Width * _viewModel.Strip.Dpi / 25.4);
                var totalScale = _viewModel.PreviewScale * imageScale;
                
                var stripX = (pos.X - _viewModel.PreviewOffsetX) / totalScale * (25.4 / _viewModel.Strip.Dpi);
                var stripY = (pos.Y - _viewModel.PreviewOffsetY) / totalScale * (25.4 / _viewModel.Strip.Dpi);

                _viewModel.PlaceCutoutAt(cutout, stripX, stripY);
            }
        }

        e.Handled = true;
    }

    private void PlacementCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null || !_viewModel.IsEnabled) return;

        var pos = e.GetPosition(PlacementCanvas);
        var placement = _viewModel.GetPlacementAtPosition(pos.X, pos.Y);
        
        if (placement != null)
        {
            _draggedPlacement = placement;
            _viewModel.SelectedPlacement = placement;
            _isDragging = true;
            
            // Calculate the visual center position in screen coordinates
            var cutout = _viewModel.Cutouts.FirstOrDefault(c => c.Id == placement.CutoutId);
            if (cutout != null && _viewModel.Strip != null && _viewModel.PreviewImage != null)
            {
                // Get visual center position in strip coordinates (mm)
                var (width, height) = GetRotatedDimensions(cutout, placement);
                double visualCenterX = placement.X;
                double visualCenterY = placement.Y;
                
                if (placement.Rotation == 0)
                {
                    visualCenterX = placement.X + width / 2;
                    visualCenterY = placement.Y + height / 2;
                }
                else if (placement.Rotation == 90)
                {
                    visualCenterX = placement.X - height / 2;
                    visualCenterY = placement.Y + width / 2;
                }
                else if (placement.Rotation == 180)
                {
                    visualCenterX = placement.X - width / 2;
                    visualCenterY = placement.Y - height / 2;
                }
                else if (placement.Rotation == 270)
                {
                    visualCenterX = placement.X + height / 2;
                    visualCenterY = placement.Y - width / 2;
                }
                
                // Convert visual center to screen coordinates
                var imageScale = _viewModel.PreviewImage.PixelWidth / (_viewModel.Strip.Width * _viewModel.Strip.Dpi / 25.4);
                var totalScale = _viewModel.PreviewScale * imageScale;
                var screenCenterX = (visualCenterX * totalScale * (_viewModel.Strip.Dpi / 25.4)) + _viewModel.PreviewOffsetX;
                var screenCenterY = (visualCenterY * totalScale * (_viewModel.Strip.Dpi / 25.4)) + _viewModel.PreviewOffsetY;
                
                // Calculate offset from mouse to center
                _dragOffsetX = pos.X - screenCenterX;
                _dragOffsetY = pos.Y - screenCenterY;
                
                // Update outline with center position
                UpdateDragOutline(screenCenterX, screenCenterY);
            }
            else
            {
                UpdateDragOutline(pos.X, pos.Y);
            }
            
            PlacementCanvas.CaptureMouse();
            
            e.Handled = true;
        }
    }

    private void PlacementCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedPlacement != null && _viewModel != null)
        {
            var pos = e.GetPosition(PlacementCanvas);
            // Adjust mouse position by drag offset to get the visual center position
            var centerX = pos.X - _dragOffsetX;
            var centerY = pos.Y - _dragOffsetY;
            
            // Use lightweight update during dragging (doesn't regenerate preview)
            // Convert screen center to strip coordinates and update placement center
            if (_viewModel.Strip != null && _viewModel.PreviewImage != null)
            {
                var imageScale = _viewModel.PreviewImage.PixelWidth / (_viewModel.Strip.Width * _viewModel.Strip.Dpi / 25.4);
                var totalScale = _viewModel.PreviewScale * imageScale;
                
                // Convert screen center to strip coordinates (mm)
                var stripCenterX = (centerX - _viewModel.PreviewOffsetX) / totalScale * (25.4 / _viewModel.Strip.Dpi);
                var stripCenterY = (centerY - _viewModel.PreviewOffsetY) / totalScale * (25.4 / _viewModel.Strip.Dpi);
                
                // Calculate what the placement origin (X, Y) should be for this center position
                var cutout = _viewModel.Cutouts.FirstOrDefault(c => c.Id == _draggedPlacement.CutoutId);
                if (cutout != null)
                {
                    var (width, height) = GetRotatedDimensions(cutout, _draggedPlacement);
                    double newX = stripCenterX;
                    double newY = stripCenterY;
                    
                    if (_draggedPlacement.Rotation == 0)
                    {
                        newX = stripCenterX - width / 2;
                        newY = stripCenterY - height / 2;
                    }
                    else if (_draggedPlacement.Rotation == 90)
                    {
                        newX = stripCenterX + height / 2;
                        newY = stripCenterY - width / 2;
                    }
                    else if (_draggedPlacement.Rotation == 180)
                    {
                        newX = stripCenterX + width / 2;
                        newY = stripCenterY + height / 2;
                    }
                    else if (_draggedPlacement.Rotation == 270)
                    {
                        newX = stripCenterX - height / 2;
                        newY = stripCenterY + width / 2;
                    }
                    
                    _viewModel.UpdatePlacementPosition(_draggedPlacement, newX, newY);
                }
            }
            
            UpdateDragOutline(centerX, centerY);
            e.Handled = true;
        }
    }

    private void PlacementCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && _draggedPlacement != null && _viewModel != null)
        {
            // Finalize the drag - regenerate preview and notify other components
            var pos = e.GetPosition(PlacementCanvas);
            // Adjust mouse position by drag offset to get the visual center position
            var centerX = pos.X - _dragOffsetX;
            var centerY = pos.Y - _dragOffsetY;
            
            // Convert screen center to strip coordinates and update placement center
            if (_viewModel.Strip != null && _viewModel.PreviewImage != null)
            {
                var imageScale = _viewModel.PreviewImage.PixelWidth / (_viewModel.Strip.Width * _viewModel.Strip.Dpi / 25.4);
                var totalScale = _viewModel.PreviewScale * imageScale;
                
                // Convert screen center to strip coordinates (mm)
                var stripCenterX = (centerX - _viewModel.PreviewOffsetX) / totalScale * (25.4 / _viewModel.Strip.Dpi);
                var stripCenterY = (centerY - _viewModel.PreviewOffsetY) / totalScale * (25.4 / _viewModel.Strip.Dpi);
                
                // Calculate what the placement origin (X, Y) should be for this center position
                var cutout = _viewModel.Cutouts.FirstOrDefault(c => c.Id == _draggedPlacement.CutoutId);
                if (cutout != null)
                {
                    var (width, height) = GetRotatedDimensions(cutout, _draggedPlacement);
                    double newX = stripCenterX;
                    double newY = stripCenterY;
                    
                    if (_draggedPlacement.Rotation == 0)
                    {
                        newX = stripCenterX - width / 2;
                        newY = stripCenterY - height / 2;
                    }
                    else if (_draggedPlacement.Rotation == 90)
                    {
                        newX = stripCenterX + height / 2;
                        newY = stripCenterY - width / 2;
                    }
                    else if (_draggedPlacement.Rotation == 180)
                    {
                        newX = stripCenterX + width / 2;
                        newY = stripCenterY + height / 2;
                    }
                    else if (_draggedPlacement.Rotation == 270)
                    {
                        newX = stripCenterX - height / 2;
                        newY = stripCenterY + width / 2;
                    }
                    
                    _viewModel.UpdatePlacement(_draggedPlacement, newX, newY);
                }
            }
            
            ClearDragOutline();
            _isDragging = false;
            _draggedPlacement = null;
            _dragOffsetX = 0;
            _dragOffsetY = 0;
            PlacementCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void UpdateDragOutline(double screenCenterX, double screenCenterY)
    {
        if (_viewModel == null || _draggedPlacement == null || _viewModel.Strip == null || _viewModel.PreviewImage == null)
        {
            ClearDragOutline();
            return;
        }

        var cutout = _viewModel.Cutouts.FirstOrDefault(c => c.Id == _draggedPlacement.CutoutId);
        if (cutout == null)
        {
            ClearDragOutline();
            return;
        }

        // Calculate outline based on visual center position
        var widthPx = (int)(_viewModel.Strip.Width * _viewModel.Strip.Dpi / 25.4);
        var scale = widthPx / _viewModel.Strip.Width; // pixels per mm in bitmap

        // Get visual dimensions after rotation (already accounts for rotation)
        var (width, height) = GetRotatedDimensions(cutout, _draggedPlacement);

        // Convert dimensions from mm to bitmap pixels
        var bitmapWidth = width * scale;
        var bitmapHeight = height * scale;

        // Convert bitmap pixels to screen pixels
        var imageScale = _viewModel.PreviewImage.PixelWidth / (double)widthPx;
        var totalScale = _viewModel.PreviewScale * imageScale;

        // Convert to screen coordinates
        var screenWidth = bitmapWidth * totalScale;
        var screenHeight = bitmapHeight * totalScale;

        // Calculate visual top-left from center
        var visualX = screenCenterX - screenWidth / 2;
        var visualY = screenCenterY - screenHeight / 2;

        // Create or update outline rectangle
        if (_dragOutline == null)
        {
            _dragOutline = new Rectangle
            {
                Stroke = System.Windows.Media.Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new System.Windows.Media.DoubleCollection(new[] { 5.0, 5.0 }),
                Fill = System.Windows.Media.Brushes.Transparent
            };
            PlacementCanvas.Children.Add(_dragOutline);
        }

        Canvas.SetLeft(_dragOutline, visualX);
        Canvas.SetTop(_dragOutline, visualY);
        _dragOutline.Width = screenWidth;
        _dragOutline.Height = screenHeight;
    }

    private void ClearDragOutline()
    {
        if (_dragOutline != null)
        {
            PlacementCanvas.Children.Remove(_dragOutline);
            _dragOutline = null;
        }
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

    private void ScaleXTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPlacement != null)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var scaleX))
            {
                _viewModel.UpdatePlacementScale(_viewModel.SelectedPlacement, scaleX, _viewModel.SelectedPlacement.ScaleY);
            }
        }
    }

    private void ScaleYTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPlacement != null)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var scaleY))
            {
                _viewModel.UpdatePlacementScale(_viewModel.SelectedPlacement, _viewModel.SelectedPlacement.ScaleX, scaleY);
            }
        }
    }

    private void XTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPlacement != null)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x))
            {
                _viewModel.UpdatePlacement(_viewModel.SelectedPlacement, x, _viewModel.SelectedPlacement.Y);
            }
            else
            {
                // If parsing fails, revert to current value
                textBox.Text = _viewModel.SelectedPlacement.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    private void YTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedPlacement != null)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
            {
                _viewModel.UpdatePlacement(_viewModel.SelectedPlacement, _viewModel.SelectedPlacement.X, y);
            }
            else
            {
                // If parsing fails, revert to current value
                textBox.Text = _viewModel.SelectedPlacement.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
