using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using LabelMeister.Core.Services;
using LabelMeister.Services.Implementations;
using LabelMeister.ViewModels;
using LabelMeister.Views;
using System.Windows.Threading;

namespace LabelMeister
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Ensure PDFium native DLLs are present before proceeding
            var pdfiumService = ServiceProvider.GetRequiredService<IPdfiumNativeService>();
            EnsurePdfiumDlls(pdfiumService);

            // Create and show main window
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void EnsurePdfiumDlls(IPdfiumNativeService pdfiumService)
        {
            // Check synchronously first (fast check)
            if (!pdfiumService.ArePdfiumNativeDllsPresent())
            {
                // Show a message to user
                var result = MessageBox.Show(
                    "PDFium native DLLs are not found. The application will attempt to locate them.\n\n" +
                    "If this fails, please ensure:\n" +
                    "1. NuGet packages are restored (run 'dotnet restore')\n" +
                    "2. The solution is rebuilt\n" +
                    "3. PdfiumViewer.Native.x86_64.v8-xfa package is installed\n\n" +
                    "Continue anyway?",
                    "PDFium DLL Check",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.No)
                {
                    Shutdown();
                    return;
                }

                // Try to ensure DLLs asynchronously
                Task.Run(async () =>
                {
                    var success = await pdfiumService.EnsurePdfiumNativeDllsAsync();
                    if (!success)
                    {
                        // Show error on UI thread
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"Failed to locate PDFium DLLs.\n\nStatus: {pdfiumService.StatusMessage}\n\n" +
                                "The application may not work correctly. Please restore NuGet packages and rebuild.",
                                "PDFium DLL Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                        });
                    }
                });
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IPdfiumNativeService, PdfiumNativeService>();
            services.AddSingleton<IPdfService, PdfService>();
            services.AddSingleton<IRasterService, RasterService>();
            services.AddSingleton<ICutoutService, CutoutService>();
            services.AddSingleton<IStripService, StripService>();
            services.AddSingleton<IPlacementService, PlacementService>();
            services.AddSingleton<IExportService, ExportService>();
            services.AddSingleton<ITemplateService, TemplateService>();

            // Register ViewModels as Singleton so MainViewModel and Views share the same instances
            services.AddSingleton<PdfUploadViewModel>();
            services.AddSingleton<RasterizeViewModel>();
            services.AddSingleton<MoveCombineViewModel>();
            services.AddSingleton<StripSelectionViewModel>();
            services.AddSingleton<PlacementViewModel>();
            services.AddSingleton<ExportViewModel>();
            services.AddTransient<MainViewModel>();

            // Register Views
            services.AddTransient<MainWindow>();
            services.AddTransient<PdfUploadView>();
            services.AddTransient<RasterizeView>();
            services.AddTransient<MoveCombineView>();
            services.AddTransient<StripSelectionView>();
            services.AddTransient<PlacementView>();
            services.AddTransient<ExportView>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
