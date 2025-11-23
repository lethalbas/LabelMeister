# PDF-to-Bitmap Library Recommendations

## Overview
These libraries convert PDF pages directly to bitmap images, preserving all visual elements (images, colors, fonts, graphics) automatically.

---

## 🏆 Top Recommendations

### 1. **PdfiumViewer** ⭐ RECOMMENDED
**NuGet Package:** `PdfiumViewer`

**Why choose this:**
- ✅ **Free and open-source** (Apache 2.0 license)
- ✅ **Simple API** - just `Render()` method
- ✅ **High quality** - uses PDFium (Google Chrome's PDF engine)
- ✅ **Active maintenance** - well-maintained project
- ✅ **Good documentation** and community support
- ✅ **Works with .NET 6.0**

**Installation:**
```bash
dotnet add package PdfiumViewer
```

**Requirements:**
- Requires native PDFium binaries (automatically included via NuGet dependency `PdfiumViewer.Native.x86_64` or `PdfiumViewer.Native.x86`)
- Works on Windows, Linux, macOS (with appropriate native binaries)

**Usage Example:**
```csharp
using PdfiumViewer;
using System.Drawing.Imaging;

public async Task<byte[]?> RasterizePageAsync(PdfDocumentModel document, int pageIndex = 0, double dpi = 300.0)
{
    return await Task.Run(() =>
    {
        using var pdfDoc = PdfDocument.Load(document.FilePath);
        
        var width = (int)(document.Width * dpi / 72.0);
        var height = (int)(document.Height * dpi / 72.0);
        
        // Render page to bitmap
        using var image = pdfDoc.Render(pageIndex, width, height, dpi, dpi, false);
        
        // Convert to PNG bytes
        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    });
}
```

**Pros:**
- Free and open-source
- Simple, clean API
- Excellent rendering quality
- Production-ready

**Cons:**
- Requires native binaries (but NuGet handles this)
- Slightly larger package size due to native dependencies

**License:** Apache 2.0 (Free)
**Rating:** ⭐⭐⭐⭐⭐

---

### 2. **MuPDFCore** ⭐ CROSS-PLATFORM ALTERNATIVE
**NuGet Package:** `MuPDFCore`

**Why choose this:**
- ✅ **Cross-platform** - works on Windows, Linux, macOS, iOS, Android
- ✅ **Fast rendering** - optimized performance
- ✅ **Free and open-source** (AGPL license, with commercial options)
- ✅ **No native dependencies** - pure .NET implementation
- ✅ **Modern API** - async/await support
- ✅ **Smaller package size** than PdfiumViewer

**Installation:**
```bash
dotnet add package MuPDFCore
```

**Usage Example:**
```csharp
using MuPDFCore;

public async Task<byte[]?> RasterizePageAsync(PdfDocumentModel document, int pageIndex = 0, double dpi = 300.0)
{
    return await Task.Run(() =>
    {
        using var pdfDoc = new MuPDFContext();
        using var doc = pdfDoc.LoadDocument(document.FilePath);
        
        var width = (int)(document.Width * dpi / 72.0);
        var height = (int)(document.Height * dpi / 72.0);
        
        // Render page
        using var pixmap = doc.RenderPage(pageIndex, width, height, dpi, dpi);
        
        // Convert to PNG bytes
        return pixmap.AsPNG();
    });
}
```

**Pros:**
- Cross-platform support
- Fast and efficient
- No native dependencies
- Modern API

**Cons:**
- AGPL license (requires open-source if used commercially, or buy commercial license)
- Less well-known than PdfiumViewer

**License:** AGPL (Free for open-source, commercial license available)
**Rating:** ⭐⭐⭐⭐⭐ (for cross-platform needs)

---

### 3. **IronPDF** 💰 COMMERCIAL OPTION
**NuGet Package:** `IronPdf`

**Why choose this:**
- ✅ **Easiest API** - very simple to use
- ✅ **Excellent documentation** and support
- ✅ **Many features** - PDF creation, editing, conversion
- ✅ **Commercial support** available
- ✅ **No native dependencies**

**Installation:**
```bash
dotnet add package IronPdf
```

**Pricing:**
- Free tier: Limited (watermarks, some restrictions)
- Lite: ~$750/year
- Professional: ~$2,200/year
- Enterprise: Custom pricing

**Usage Example:**
```csharp
using IronPdf;

public async Task<byte[]?> RasterizePageAsync(PdfDocumentModel document, int pageIndex = 0, double dpi = 300.0)
{
    return await Task.Run(() =>
    {
        var pdf = PdfDocument.FromFile(document.FilePath);
        
        // Render page to image
        var image = pdf.RasterizeToImage(pageIndex, dpi);
        
        // Convert to PNG bytes
        using var ms = new MemoryStream();
        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    });
}
```

**Pros:**
- Very easy to use
- Comprehensive feature set
- Good commercial support

**Cons:**
- Expensive licensing
- Free tier has limitations

**License:** Commercial (paid)
**Rating:** ⭐⭐⭐⭐ (if budget allows)

---

## Other Options (Less Recommended)

### 4. **Syncfusion PDF Library**
**NuGet Package:** `Syncfusion.Pdf.Net`

**Details:**
- Commercial library with free community license (for small companies)
- Comprehensive PDF library
- `ExportAsImage()` method for conversion
- Good documentation

**License:** Commercial (free community license available)
**Rating:** ⭐⭐⭐

---

### 5. **PDF Focus .Net**
**NuGet Package:** `SautinSoft.PdfFocus`

**Details:**
- Commercial library
- Good PDF conversion features
- Supports various image formats

**License:** Commercial (paid)
**Rating:** ⭐⭐⭐

---

## Comparison Table

| Library | License | Cost | Ease of Use | Quality | Cross-Platform | Native Deps | Recommendation |
|---------|---------|------|-------------|---------|-----------------|-------------|----------------|
| **PdfiumViewer** | Apache 2.0 | Free | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ✅ | ✅ (auto) | ⭐⭐⭐⭐⭐ **Best overall** |
| **MuPDFCore** | AGPL/Commercial | Free* | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ✅✅ | ❌ | ⭐⭐⭐⭐⭐ **Best cross-platform** |
| **IronPDF** | Commercial | Paid | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ✅ | ❌ | ⭐⭐⭐⭐ **If budget allows** |
| Syncfusion | Commercial | Free* | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ❌ | ⭐⭐⭐ |
| PDF Focus | Commercial | Paid | ⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ❌ | ⭐⭐⭐ |

*Free with limitations/restrictions

---

## Final Recommendation

### For Your Project (.NET 6.0, Windows-focused):

**🥇 Primary Choice: PdfiumViewer**
- Best balance of simplicity, quality, and cost (free)
- Well-documented and actively maintained
- Native dependencies handled automatically via NuGet
- Perfect for Windows development

**🥈 Alternative: MuPDFCore**
- Choose if you need cross-platform support
- Also free (check AGPL license requirements)
- No native dependencies needed

**🥉 If Budget Allows: IronPDF**
- Simplest API
- Best commercial support
- Expensive but feature-rich

---

## Implementation Steps (Using PdfiumViewer)

1. **Install the package:**
   ```bash
   dotnet add LabelMeister.Services package PdfiumViewer
   ```

2. **Update PdfService.cs:**
   - Add `using PdfiumViewer;`
   - Replace `RasterizePageAsync` implementation with PdfiumViewer's `Render()` method

3. **Test with sample PDFs:**
   - Verify images are preserved
   - Verify colors are preserved
   - Verify fonts are preserved

4. **Handle edge cases:**
   - Large PDFs (memory management)
   - Invalid PDFs (error handling)
   - Multiple pages (if needed)

---

## Code Integration Example

Here's how to integrate PdfiumViewer into your existing `PdfService.cs`:

```csharp
using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using PdfiumViewer;  // Add this
using System.Drawing.Imaging;  // Add this
using System.IO;

namespace LabelMeister.Services.Implementations;

public class PdfService : IPdfService
{
    // ... existing LoadPdfAsync method stays the same ...

    public async Task<byte[]?> RasterizePageAsync(PdfDocumentModel document, int pageIndex = 0, double dpi = 300.0)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var pdfDoc = PdfDocument.Load(document.FilePath);
                
                // Calculate dimensions at specified DPI
                var width = (int)(document.Width * dpi / 72.0);
                var height = (int)(document.Height * dpi / 72.0);
                
                // Render page directly to bitmap - preserves everything!
                using var image = pdfDoc.Render(pageIndex, width, height, dpi, dpi, false);
                
                // Convert to PNG bytes
                using var ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                // Log error if needed
                return null;
            }
        });
    }

    // ... rest of the class stays the same ...
}
```

**That's it!** Much simpler than the current manual text rendering approach.

---

## Notes

- **DPI Support:** All recommended libraries support custom DPI (your current 300 DPI requirement)
- **Memory:** Large PDFs may use significant memory - consider streaming for very large files
- **Performance:** PdfiumViewer and MuPDFCore are both performant; PdfiumViewer may be slightly faster
- **Licensing:** Verify license compatibility with your project's needs before final decision

