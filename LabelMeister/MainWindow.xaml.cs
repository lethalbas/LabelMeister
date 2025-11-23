using System.Windows;
using System.Windows.Controls;
using LabelMeister.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LabelMeister;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _isUpdatingSelection = false;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Inject views into the tab control
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        if (serviceProvider != null)
        {
            PdfUploadTab.Content = serviceProvider.GetRequiredService<Views.PdfUploadView>();
            RasterizeTab.Content = serviceProvider.GetRequiredService<Views.RasterizeView>();
            MoveCombineTab.Content = serviceProvider.GetRequiredService<Views.MoveCombineView>();
            StripSelectionTab.Content = serviceProvider.GetRequiredService<Views.StripSelectionView>();
            PlacementTab.Content = serviceProvider.GetRequiredService<Views.PlacementView>();
            ExportTab.Content = serviceProvider.GetRequiredService<Views.ExportView>();
        }
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection || _viewModel == null)
            return;

        var tabControl = sender as TabControl;
        if (tabControl == null)
            return;

        var selectedIndex = tabControl.SelectedIndex;
        
        // Check if the selected tab is enabled
        bool isTabEnabled = selectedIndex switch
        {
            0 => true, // PDF Upload is always enabled
            1 => _viewModel.IsTab0Completed,
            2 => _viewModel.IsTab1Completed,
            3 => _viewModel.IsTab2Completed,
            4 => _viewModel.IsTab3Completed,
            5 => _viewModel.IsTab4Completed,
            _ => false
        };

        // If tab is not enabled, revert to previous selection
        if (!isTabEnabled)
        {
            _isUpdatingSelection = true;
            tabControl.SelectedIndex = _viewModel.SelectedTabIndex;
            _isUpdatingSelection = false;
            e.Handled = true;
        }
        else
        {
            // Update view model if navigation is valid
            _viewModel.NavigateToTabCommand.Execute(selectedIndex);
        }
    }
}
