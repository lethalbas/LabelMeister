using LabelMeister.Core.Models;

namespace LabelMeister.Core.Services;

/// <summary>
/// Service for saving/loading templates (future feature)
/// </summary>
public interface ITemplateService
{
    Task SaveTemplateAsync(TemplateModel template, string filePath);
    Task<TemplateModel?> LoadTemplateAsync(string filePath);
    Task<List<TemplateModel>> ListTemplatesAsync(string directory);
}

