using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelMeister.Core.Models;
using System.Collections.ObjectModel;

namespace LabelMeister.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    // Completion tracking
    [ObservableProperty]
    private bool _isTab0Completed = false; // PDF Upload
    [ObservableProperty]
    private bool _isTab1Completed = false; // Rasterize
    [ObservableProperty]
    private bool _isTab2Completed = false; // Move & Combine
    [ObservableProperty]
    private bool _isTab3Completed = false; // Strip Selection
    [ObservableProperty]
    private bool _isTab4Completed = false; // Placement

    // Shared state
    public PdfDocumentModel? CurrentPdf { get; set; }
    public RasterGridModel? CurrentGrid { get; set; }
    public ObservableCollection<CutoutRegionModel> CutoutRegions { get; set; } = new();
    public StripModel? SelectedStrip { get; set; }
    public ObservableCollection<PlacementModel> Placements { get; set; } = new();
    public ExportSettingsModel ExportSettings { get; set; } = new();

    // ViewModels
    private readonly PdfUploadViewModel _pdfUploadViewModel;
    private readonly RasterizeViewModel _rasterizeViewModel;
    private readonly MoveCombineViewModel _moveCombineViewModel;
    private readonly StripSelectionViewModel _stripSelectionViewModel;
    private readonly PlacementViewModel _placementViewModel;
    private readonly ExportViewModel _exportViewModel;

    public MainViewModel(
        PdfUploadViewModel pdfUploadViewModel,
        RasterizeViewModel rasterizeViewModel,
        MoveCombineViewModel moveCombineViewModel,
        StripSelectionViewModel stripSelectionViewModel,
        PlacementViewModel placementViewModel,
        ExportViewModel exportViewModel)
    {
        _pdfUploadViewModel = pdfUploadViewModel;
        _rasterizeViewModel = rasterizeViewModel;
        _moveCombineViewModel = moveCombineViewModel;
        _stripSelectionViewModel = stripSelectionViewModel;
        _placementViewModel = placementViewModel;
        _exportViewModel = exportViewModel;

        // Wire up events
        _pdfUploadViewModel.PdfLoaded += OnPdfLoaded;
        _rasterizeViewModel.GridCreated += OnGridCreated;
        _rasterizeViewModel.GridUpdated += OnGridUpdated;
        _moveCombineViewModel.CutoutsChanged += OnCutoutsChanged;
        _stripSelectionViewModel.StripSelected += OnStripSelected;
        _placementViewModel.SetPlacementsChangedCallback(UpdatePlacements);

        // Wire up completion events
        _pdfUploadViewModel.CompletionChanged += OnTab0CompletionChanged;
        _rasterizeViewModel.CompletionChanged += OnTab1CompletionChanged;
        _moveCombineViewModel.CompletionChanged += OnTab2CompletionChanged;
        _stripSelectionViewModel.CompletionChanged += OnTab3CompletionChanged;
        _placementViewModel.CompletionChanged += OnTab4CompletionChanged;

        // Set initial view
        CurrentViewModel = _pdfUploadViewModel;
        
        // Wire up placement and export data
        _placementViewModel.SetCutouts(CutoutRegions.ToList());
        _exportViewModel.SetCutouts(CutoutRegions.ToList());
        _exportViewModel.SetPlacements(Placements.ToList());

        // Update enabled states
        UpdateTabEnabledStates();
    }

    [RelayCommand]
    private void NavigateToTab(int index)
    {
        // Check if the tab is enabled before navigating
        bool isTabEnabled = index switch
        {
            0 => true, // PDF Upload is always enabled
            1 => IsTab0Completed,
            2 => IsTab1Completed,
            3 => IsTab2Completed,
            4 => IsTab3Completed,
            5 => IsTab4Completed,
            _ => false
        };

        if (!isTabEnabled)
            return; // Don't navigate to disabled tabs

        SelectedTabIndex = index;
        CurrentViewModel = index switch
        {
            0 => _pdfUploadViewModel,
            1 => _rasterizeViewModel,
            2 => _moveCombineViewModel,
            3 => _stripSelectionViewModel,
            4 => _placementViewModel,
            5 => _exportViewModel,
            _ => _pdfUploadViewModel
        };
    }

    private void OnPdfLoaded(PdfDocumentModel pdf)
    {
        CurrentPdf = pdf;
        _rasterizeViewModel.SetPdfDocument(pdf);
    }

    private void OnGridCreated(RasterGridModel grid)
    {
        CurrentGrid = grid;
        _moveCombineViewModel.SetGrid(grid);
        if (CurrentPdf != null)
        {
            _moveCombineViewModel.SetPdfDocument(CurrentPdf);
        }
    }

    private void OnGridUpdated(RasterGridModel grid)
    {
        CurrentGrid = grid;
        // Update the grid reference in MoveCombineViewModel (same object instance, but ensure it's synced)
        _moveCombineViewModel.SetGrid(grid);
        // Regenerate cutouts when grid lines are updated (regenerate even if not enabled yet)
        if (_moveCombineViewModel.PdfDocument != null)
        {
            _moveCombineViewModel.GenerateCutouts();
        }
    }

    private void OnCutoutsChanged(List<CutoutRegionModel> cutouts)
    {
        CutoutRegions.Clear();
        foreach (var cutout in cutouts)
        {
            CutoutRegions.Add(cutout);
        }
        _placementViewModel.SetCutouts(cutouts);
        _exportViewModel.SetCutouts(cutouts);
    }

    private void OnStripSelected(StripModel strip)
    {
        SelectedStrip = strip;
        _placementViewModel.SetStrip(strip);
        _exportViewModel.SetStrip(strip);
    }
    
    public void UpdatePlacements(List<PlacementModel> placements)
    {
        Placements.Clear();
        foreach (var placement in placements)
        {
            Placements.Add(placement);
        }
        _exportViewModel.SetPlacements(placements);
    }

    private void UpdateTabEnabledStates()
    {
        _rasterizeViewModel.SetEnabled(IsTab0Completed);
        _moveCombineViewModel.SetEnabled(IsTab1Completed);
        _stripSelectionViewModel.SetEnabled(IsTab2Completed);
        _placementViewModel.SetEnabled(IsTab3Completed);
        _exportViewModel.SetEnabled(IsTab4Completed);
    }

    partial void OnIsTab0CompletedChanged(bool value)
    {
        UpdateTabEnabledStates();
    }

    partial void OnIsTab1CompletedChanged(bool value)
    {
        UpdateTabEnabledStates();
    }

    partial void OnIsTab2CompletedChanged(bool value)
    {
        UpdateTabEnabledStates();
    }

    partial void OnIsTab3CompletedChanged(bool value)
    {
        UpdateTabEnabledStates();
    }

    partial void OnIsTab4CompletedChanged(bool value)
    {
        UpdateTabEnabledStates();
    }

    private void OnTab0CompletionChanged(bool completed)
    {
        IsTab0Completed = completed;
    }

    private void OnTab1CompletionChanged(bool completed)
    {
        IsTab1Completed = completed;
    }

    private void OnTab2CompletionChanged(bool completed)
    {
        IsTab2Completed = completed;
    }

    private void OnTab3CompletionChanged(bool completed)
    {
        IsTab3Completed = completed;
    }

    private void OnTab4CompletionChanged(bool completed)
    {
        IsTab4Completed = completed;
    }
}

