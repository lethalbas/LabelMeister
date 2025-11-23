using LabelMeister.Core.Models;
using LabelMeister.Core.Services;
using System.Text.Json;

namespace LabelMeister.Services.Implementations;

public class TemplateService : ITemplateService
{
    public async Task SaveTemplateAsync(TemplateModel template, string filePath)
    {
        var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<TemplateModel?> LoadTemplateAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<TemplateModel>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<TemplateModel>> ListTemplatesAsync(string directory)
    {
        var templates = new List<TemplateModel>();

        if (!Directory.Exists(directory))
            return templates;

        var files = Directory.GetFiles(directory, "*.json");
        
        foreach (var file in files)
        {
            var template = await LoadTemplateAsync(file);
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates;
    }
}

