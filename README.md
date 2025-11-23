# LabelMeister

A modern WPF application for processing and managing PDF labels through a streamlined multi-step workflow.

## 🎯 Overview

LabelMeister is a desktop application built with .NET 6.0 and WPF that provides a comprehensive solution for processing PDF label documents. The application guides users through a step-by-step workflow to upload, rasterize, manipulate, and export label designs.

## ✨ Features

- **PDF Upload & Processing**: Upload and process PDF label documents with support for multiple pages
- **Rasterization**: Convert PDF pages to high-quality raster images for manipulation
- **Move & Combine**: Intuitive tools for moving and combining label regions
- **Strip Selection**: Select and manage label strips from rasterized images
- **Placement Management**: Precise placement control for label positioning
- **Export Functionality**: Export processed labels in various formats
- **Template System**: Save and load label templates for reuse
- **Modern UI**: Clean, modern interface built with ModernWpfUI

## 🏗️ Architecture

The application follows a clean architecture pattern with clear separation of concerns:

- **LabelMeister.Core**: Core domain models and service interfaces
- **LabelMeister.Services**: Service implementations for PDF processing, rasterization, and export
- **LabelMeister**: Main WPF application with views and view models
- **LabelMeister.Tests**: Unit tests for service implementations

### Key Technologies

- **.NET 6.0**: Modern .NET framework
- **WPF**: Windows Presentation Foundation for the UI
- **ModernWpfUI**: Modern UI components and styling
- **CommunityToolkit.Mvvm**: MVVM pattern implementation
- **Microsoft.Extensions.DependencyInjection**: Dependency injection container
- **PdfiumViewer**: PDF rendering and processing
- **SkiaSharp**: High-performance 2D graphics
- **PdfSharp**: PDF manipulation

## 📋 Prerequisites

- **.NET 6.0 SDK** or later
- **Windows 10/11** (WPF application)
- **Visual Studio 2022** or **Visual Studio Code** (recommended for development)

## 🚀 Getting Started

### Clone the Repository

```bash
git clone https://github.com/lethalbas/LabelMeister.git
cd LabelMeister
```

### Restore NuGet Packages

```bash
dotnet restore
```

### Build the Solution

```bash
dotnet build
```

### Run the Application

```bash
dotnet run --project LabelMeister/LabelMeister.csproj
```

Or open `LabelMeister.sln` in Visual Studio and press F5.

## 📁 Project Structure

```
LabelMeister/
├── LabelMeister/                    # Main WPF application
│   ├── Views/                       # XAML views
│   ├── ViewModels/                  # MVVM view models
│   ├── Converters/                  # Value converters
│   └── App.xaml                     # Application entry point
├── LabelMeister.Core/               # Core domain layer
│   ├── Models/                      # Domain models
│   └── Services/                    # Service interfaces
├── LabelMeister.Services/           # Service implementations
│   └── Implementations/             # Service implementations
└── LabelMeister.Tests/              # Unit tests
```

## 🔧 Development

### Running Tests

```bash
dotnet test
```

### Building for Release

```bash
dotnet build -c Release
```

## 📝 Workflow

The application follows a sequential workflow:

1. **PDF Upload**: Upload your PDF label document
2. **Rasterize**: Convert PDF pages to raster images
3. **Move & Combine**: Manipulate label regions
4. **Strip Selection**: Select strips from the processed images
5. **Placement**: Configure label placement settings
6. **Export**: Export the final result

Each step must be completed before proceeding to the next, ensuring data integrity throughout the process.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Built with modern .NET technologies
- Uses PDFium for PDF processing
- UI powered by ModernWpfUI

## 📧 Contact

For questions or support, please open an issue on GitHub.

---

**Note**: This application requires PDFium native DLLs to function properly. The build process automatically handles copying these dependencies, but ensure NuGet packages are restored before building.

