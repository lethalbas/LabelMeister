namespace LabelMeister.Core.Services;

/// <summary>
/// Service for ensuring PDFium native DLLs are present and valid
/// </summary>
public interface IPdfiumNativeService
{
    /// <summary>
    /// Ensures PDFium native DLLs are present, downloading them if necessary
    /// </summary>
    /// <returns>True if DLLs are available, false otherwise</returns>
    Task<bool> EnsurePdfiumNativeDllsAsync();

    /// <summary>
    /// Checks if PDFium native DLLs are present
    /// </summary>
    /// <returns>True if DLLs are present, false otherwise</returns>
    bool ArePdfiumNativeDllsPresent();

    /// <summary>
    /// Gets the status message about PDFium DLL availability
    /// </summary>
    string StatusMessage { get; }

    /// <summary>
    /// Initializes PDFium DLL by explicitly loading it before PdfiumViewer tries to use it
    /// This must be called before any PdfiumViewer operations to prevent DllNotFoundException
    /// </summary>
    /// <returns>True if DLL was initialized successfully, false otherwise</returns>
    bool InitializePdfiumDll();
}

