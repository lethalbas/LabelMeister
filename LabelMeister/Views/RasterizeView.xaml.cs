using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using LabelMeister.ViewModels;

namespace LabelMeister.Views;

public partial class RasterizeView : UserControl
{
    private RasterizeViewModel? _viewModel;
    private (bool isRow, int index)? _draggedLine = null;
    private bool _isDragging = false;

    public RasterizeView(RasterizeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        
        // Subscribe to image loaded event to update canvas size
        PreviewImageControl.Loaded += PreviewImageControl_Loaded;
        PreviewImageControl.ImageFailed += (s, e) => UpdateCanvasSize();
        
        // Update canvas when preview image or grid changes
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RasterizeViewModel.PreviewImage))
                {
                    UpdateCanvasSize();
                    UpdateGridLines();
                }
                else if (e.PropertyName == nameof(RasterizeViewModel.Grid))
                {
                    UpdateGridLines();
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
        UpdateGridLines();
    }

    private void UpdateCanvasSize()
    {
        if (PreviewImageControl.Source is System.Windows.Media.Imaging.BitmapImage bitmap && _viewModel != null)
        {
            // Match canvas size to the border container (accounting for padding)
            var borderPadding = PreviewBorder.Padding;
            var availableWidth = PreviewBorder.ActualWidth - borderPadding.Left - borderPadding.Right;
            var availableHeight = PreviewBorder.ActualHeight - borderPadding.Top - borderPadding.Bottom;
            
            GridCanvas.Width = availableWidth > 0 ? availableWidth : PreviewBorder.ActualWidth;
            GridCanvas.Height = availableHeight > 0 ? availableHeight : PreviewBorder.ActualHeight;
            
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
                _viewModel.PreviewOffsetX = borderPadding.Left + (GridCanvas.Width - displayedWidth) / 2;
                _viewModel.PreviewOffsetY = borderPadding.Top + (GridCanvas.Height - displayedHeight) / 2;
            }
            
            UpdateGridLines();
        }
    }

    private void UpdateGridLines()
    {
        GridCanvas.Children.Clear();
        
        if (_viewModel?.Grid == null || _viewModel.PreviewImage == null || _viewModel.PdfDocument == null)
            return;

        var imageScale = _viewModel.PreviewImage.PixelWidth / _viewModel.PdfDocument.Width;
        var scale = _viewModel.PreviewScale * imageScale;

        // Draw row lines
        for (int i = 0; i < _viewModel.Grid.RowPositions.Count; i++)
        {
            var rowPos = _viewModel.Grid.RowPositions[i];
            var y = _viewModel.PreviewOffsetY + (float)(rowPos * scale);
            
            var line = new Line
            {
                X1 = _viewModel.PreviewOffsetX,
                Y1 = y,
                X2 = _viewModel.PreviewOffsetX + (_viewModel.PreviewImage.PixelWidth * _viewModel.PreviewScale),
                Y2 = y,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 3,
                Cursor = Cursors.SizeNS,
                Tag = new { IsRow = true, Index = i }
            };
            
            GridCanvas.Children.Add(line);
        }

        // Draw column lines
        for (int i = 0; i < _viewModel.Grid.ColumnPositions.Count; i++)
        {
            var colPos = _viewModel.Grid.ColumnPositions[i];
            var x = _viewModel.PreviewOffsetX + (float)(colPos * scale);
            
            var line = new Line
            {
                X1 = x,
                Y1 = _viewModel.PreviewOffsetY,
                X2 = x,
                Y2 = _viewModel.PreviewOffsetY + (_viewModel.PreviewImage.PixelHeight * _viewModel.PreviewScale),
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 3,
                Cursor = Cursors.SizeWE,
                Tag = new { IsRow = false, Index = i }
            };
            
            GridCanvas.Children.Add(line);
        }
    }

    private void GridCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null || !_viewModel.IsEnabled) return;

        var pos = e.GetPosition(GridCanvas);
        var lineInfo = _viewModel.GetGridLineAtPosition(pos.X, pos.Y);
        
        if (lineInfo.HasValue)
        {
            _draggedLine = lineInfo.Value;
            _isDragging = true;
            GridCanvas.CaptureMouse();
            e.Handled = true;
        }
    }

    private void GridCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedLine.HasValue && _viewModel != null)
        {
            var pos = e.GetPosition(GridCanvas);
            
            // Calculate the new position without regenerating preview
            if (_viewModel.Grid == null || _viewModel.PdfDocument == null || _viewModel.PreviewImage == null)
            {
                return;
            }

            var imageScale = _viewModel.PreviewImage.PixelWidth / _viewModel.PdfDocument.Width;
            var totalScale = _viewModel.PreviewScale * imageScale;
            
            double position;
            if (_draggedLine.Value.isRow)
            {
                var pdfY = (pos.Y - _viewModel.PreviewOffsetY) / totalScale;
                position = Math.Clamp(pdfY, 0, _viewModel.Grid.CanvasHeight);
            }
            else
            {
                var pdfX = (pos.X - _viewModel.PreviewOffsetX) / totalScale;
                position = Math.Clamp(pdfX, 0, _viewModel.Grid.CanvasWidth);
            }

            // Update grid position without regenerating preview (performance optimization)
            _viewModel.UpdateGridLinePosition(
                _draggedLine.Value.isRow,
                _draggedLine.Value.index,
                position
            );
            
            // Only update the Canvas lines (cheap operation)
            UpdateGridLines();
            e.Handled = true;
        }
    }

    private void GridCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && _draggedLine.HasValue && _viewModel != null)
        {
            // Finalize the drag - regenerate preview and notify other components
            var pos = e.GetPosition(GridCanvas);
            _viewModel.UpdateGridLineFromScreenPosition(
                _draggedLine.Value.isRow,
                _draggedLine.Value.index,
                pos.X,
                pos.Y
            );
            
            _isDragging = false;
            _draggedLine = null;
            GridCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void GridCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedLine.HasValue && _viewModel != null)
        {
            // Finalize the drag - regenerate preview and notify other components
            var pos = e.GetPosition(GridCanvas);
            _viewModel.UpdateGridLineFromScreenPosition(
                _draggedLine.Value.isRow,
                _draggedLine.Value.index,
                pos.X,
                pos.Y
            );
            
            _isDragging = false;
            _draggedLine = null;
            GridCanvas.ReleaseMouseCapture();
        }
    }
}

