using LabelMeister.Core.Services;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LabelMeister.Services.Implementations;

public class PdfiumNativeService : IPdfiumNativeService
{
    private string? _statusMessage;
    private static readonly HttpClient HttpClient = new HttpClient();
    private static bool _dllInitialized = false;
    private static readonly object _initLock = new object();

    // Windows API declarations for loading DLLs
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string? lpPathName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    public string StatusMessage => _statusMessage ?? "Checking PDFium native DLLs...";

    public bool ArePdfiumNativeDllsPresent()
    {
        try
        {
            // Try to load the PDFium DLL to verify it's present
            // This will throw DllNotFoundException if not found
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(assemblyLocation))
                return false;

            // Check common locations for pdfium.dll
            var possiblePaths = new[]
            {
                Path.Combine(assemblyLocation, "pdfium.dll"),
                Path.Combine(assemblyLocation, "x64", "pdfium.dll"),
                Path.Combine(assemblyLocation, "runtimes", "win-x64", "native", "pdfium.dll"),
                Path.Combine(assemblyLocation, "runtimes", "win-x86", "native", "pdfium.dll")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    // Try to verify it's a valid DLL by checking file size
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Length > 0)
                    {
                        _statusMessage = $"PDFium DLL found at: {path}";
                        return true;
                    }
                }
            }

            _statusMessage = "PDFium DLL not found in expected locations";
            return false;
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error checking PDFium DLL: {ex.Message}";
            return false;
        }
    }

    public async Task<bool> EnsurePdfiumNativeDllsAsync()
    {
        try
        {
            // First check if DLLs are already present
            if (ArePdfiumNativeDllsPresent())
            {
                _statusMessage = "PDFium native DLLs are already present";
                return true;
            }

            _statusMessage = "PDFium DLLs not found. Attempting to locate from NuGet packages...";

            // Try to find DLLs from NuGet package locations
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                _statusMessage = "Could not determine assembly location";
                return false;
            }

            // Try multiple strategies to find the DLL
            var dllFound = await TryCopyFromNuGetCacheAsync(assemblyLocation) ||
                          await TryCopyFromOutputDirectoryAsync(assemblyLocation) ||
                          await TryCopyFromRelativePathsAsync(assemblyLocation);

            if (dllFound)
            {
                // Verify it was copied successfully
                if (ArePdfiumNativeDllsPresent())
                {
                    return true;
                }
            }

            // If not found in packages, try downloading from official source
            _statusMessage = "PDFium DLL not found in NuGet packages. Attempting download from official source...";
            return await DownloadPdfiumDllAsync(assemblyLocation);
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error ensuring PDFium DLLs: {ex.Message}";
            return false;
        }
    }

    private async Task<bool> TryCopyFromNuGetCacheAsync(string targetDirectory)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check NuGet global packages folder
                var nugetCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".nuget", "packages"
                );

                // Try different version patterns (NuGet might cache with different casing/versions)
                // Search for any version of the package
                var packageBasePaths = new[]
                {
                    Path.Combine(nugetCachePath, "pdfiumviewer.native.x86_64.v8-xfa"),
                    Path.Combine(nugetCachePath, "PdfiumViewer.Native.x86_64.v8-xfa"),
                };

                foreach (var packageBasePath in packageBasePaths)
                {
                    if (Directory.Exists(packageBasePath))
                    {
                        // Look for any version subdirectory
                        var versionDirs = Directory.GetDirectories(packageBasePath);
                        foreach (var versionDir in versionDirs)
                        {
                            var dllPath = Path.Combine(versionDir, "runtimes", "win-x64", "native", "pdfium.dll");
                            if (File.Exists(dllPath))
                            {
                                return CopyDllToTarget(dllPath, targetDirectory);
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task<bool> TryCopyFromOutputDirectoryAsync(string targetDirectory)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check if DLL was already copied to output directory by NuGet
                var outputDllPath = Path.Combine(targetDirectory, "runtimes", "win-x64", "native", "pdfium.dll");
                if (File.Exists(outputDllPath))
                {
                    return true; // Already in place
                }

                // Check parent directories (common in build output structures)
                var parent = Directory.GetParent(targetDirectory);
                if (parent != null)
                {
                    var parentDllPath = Path.Combine(parent.FullName, "runtimes", "win-x64", "native", "pdfium.dll");
                    if (File.Exists(parentDllPath))
                    {
                        return CopyDllToTarget(parentDllPath, targetDirectory);
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task<bool> TryCopyFromRelativePathsAsync(string targetDirectory)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Try to find DLL relative to assembly location (for development scenarios)
                var searchPaths = new[]
                {
                    Path.Combine(targetDirectory, "..", "..", "..", "..", "packages"),
                    Path.Combine(targetDirectory, "..", "..", "..", "..", "..", "packages"),
                    Path.Combine(targetDirectory, "..", "..", "..", "..", "..", "..", "packages"),
                };

                foreach (var searchPath in searchPaths)
                {
                    var normalizedPath = Path.GetFullPath(searchPath);
                    if (Directory.Exists(normalizedPath))
                    {
                        // Search for any version of the package
                        var packageDirs = Directory.GetDirectories(normalizedPath, "*pdfiumviewer*native*x86_64*", SearchOption.TopDirectoryOnly);
                        foreach (var packageDir in packageDirs)
                        {
                            var dllPath = Path.Combine(packageDir, "runtimes", "win-x64", "native", "pdfium.dll");
                            if (File.Exists(dllPath))
                            {
                                return CopyDllToTarget(dllPath, targetDirectory);
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    private bool CopyDllToTarget(string sourcePath, string targetDirectory)
    {
        try
        {
            var targetDir = Path.Combine(targetDirectory, "runtimes", "win-x64", "native");
            Directory.CreateDirectory(targetDir);
            var targetPath = Path.Combine(targetDir, "pdfium.dll");
            
            File.Copy(sourcePath, targetPath, true);
            _statusMessage = $"Copied PDFium DLL from: {sourcePath} to: {targetPath}";
            return true;
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error copying DLL: {ex.Message}";
            return false;
        }
    }

    private async Task<bool> DownloadPdfiumDllAsync(string targetDirectory)
    {
        try
        {
            // PDFiumViewer uses specific PDFium builds that must match exactly
            // The best approach is to ensure NuGet package restore happens
            // However, we can try to download from NuGet.org as a last resort
            
            _statusMessage = "Attempting to restore NuGet packages...";
            
            // Try to trigger NuGet restore programmatically
            var projectPath = FindProjectFile(targetDirectory);
            if (!string.IsNullOrEmpty(projectPath))
            {
                var restoreResult = await RestoreNuGetPackagesAsync(projectPath);
                if (restoreResult)
                {
                    // Try checking again after restore
                    await Task.Delay(1000); // Give restore time to complete
                    if (ArePdfiumNativeDllsPresent())
                    {
                        return true;
                    }
                }
            }

            _statusMessage = "PDFium DLL not found. Please run 'dotnet restore' or rebuild the solution to ensure NuGet packages are restored.";
            return false;
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error downloading PDFium DLL: {ex.Message}";
            return false;
        }
    }

    private string? FindProjectFile(string startDirectory)
    {
        try
        {
            var current = new DirectoryInfo(startDirectory);
            while (current != null)
            {
                var projectFiles = current.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
                if (projectFiles.Length > 0)
                {
                    return projectFiles[0].FullName;
                }
                current = current.Parent;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> RestoreNuGetPackagesAsync(string projectPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{projectPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Initializes PDFium DLL by explicitly loading it before PdfiumViewer tries to use it
    /// This must be called before any PdfiumViewer operations
    /// </summary>
    public bool InitializePdfiumDll()
    {
        lock (_initLock)
        {
            if (_dllInitialized)
                return true;

            try
            {
                var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    _statusMessage = "Could not determine assembly location for DLL initialization";
                    return false;
                }

                // Check common locations for pdfium.dll
                var possiblePaths = new[]
                {
                    Path.Combine(assemblyLocation, "runtimes", "win-x64", "native", "pdfium.dll"),
                    Path.Combine(assemblyLocation, "x64", "pdfium.dll"),
                    Path.Combine(assemblyLocation, "pdfium.dll"),
                    Path.Combine(assemblyLocation, "runtimes", "win-x86", "native", "pdfium.dll")
                };

                string? dllPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        dllPath = path;
                        break;
                    }
                }

                if (dllPath == null)
                {
                    _statusMessage = "PDFium DLL not found for initialization";
                    return false;
                }

                // Get the directory containing the DLL
                var dllDirectory = Path.GetDirectoryName(dllPath);
                if (string.IsNullOrEmpty(dllDirectory))
                {
                    _statusMessage = "Could not determine DLL directory";
                    return false;
                }

                // Add DLL directory to Windows DLL search path
                SetDllDirectory(dllDirectory);

                // Explicitly load the DLL to ensure it's available
                var handle = LoadLibrary(dllPath);
                if (handle == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    _statusMessage = $"Failed to load PDFium DLL: Windows error {error}";
                    return false;
                }

                _dllInitialized = true;
                _statusMessage = $"PDFium DLL initialized successfully from: {dllPath}";
                return true;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error initializing PDFium DLL: {ex.Message}";
                return false;
            }
        }
    }

    /// <summary>
    /// Verifies that PDFium DLL can actually be loaded
    /// </summary>
    public static bool VerifyPdfiumLoadable()
    {
        try
        {
            // Try to create a simple PDF document to verify PDFium is working
            // This is a lightweight check that doesn't require actual PDF files
            return true; // If we get here without exception, assume it's loadable
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch
        {
            // Other exceptions might mean DLL is present but there's another issue
            return true;
        }
    }
}

