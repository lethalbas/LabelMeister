using System.Windows.Controls;
using LabelMeister.ViewModels;

namespace LabelMeister.Views;

public partial class PdfUploadView : UserControl
{
    public PdfUploadView(PdfUploadViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

