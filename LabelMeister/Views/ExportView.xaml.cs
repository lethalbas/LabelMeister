using System.Windows.Controls;
using LabelMeister.ViewModels;

namespace LabelMeister.Views;

public partial class ExportView : UserControl
{
    public ExportView(ExportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

