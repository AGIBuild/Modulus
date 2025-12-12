using System.Reflection;
using System.Text;

namespace Modulus.Cli.Templates;

/// <summary>
/// Engine for generating module projects from templates.
/// </summary>
public class TemplateEngine
{
    private readonly ModuleTemplateContext _context;

    public TemplateEngine(ModuleTemplateContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generate the module project structure.
    /// </summary>
    public async Task GenerateAsync(string outputPath)
    {
        var moduleDir = Path.Combine(outputPath, _context.ModuleName);
        
        // Create Core project
        await GenerateCoreProjectAsync(moduleDir);
        
        // Create UI project based on target host
        if (_context.TargetHost == TargetHostType.Avalonia)
        {
            await GenerateAvaloniaProjectAsync(moduleDir);
        }
        else
        {
            await GenerateBlazorProjectAsync(moduleDir);
        }
        
        // Create extension.vsixmanifest
        await GenerateManifestAsync(moduleDir);
    }

    private async Task GenerateCoreProjectAsync(string moduleDir)
    {
        var coreDir = Path.Combine(moduleDir, $"{_context.ModuleName}.Core");
        var viewModelsDir = Path.Combine(coreDir, "ViewModels");
        
        Directory.CreateDirectory(viewModelsDir);
        
        await WriteTemplateAsync("Core/Core.csproj.template", 
            Path.Combine(coreDir, $"{_context.ModuleName}.Core.csproj"));
        
        await WriteTemplateAsync("Core/Module.cs.template", 
            Path.Combine(coreDir, $"{_context.ModuleName}Module.cs"));
        
        await WriteTemplateAsync("Core/ViewModels/MainViewModel.cs.template", 
            Path.Combine(viewModelsDir, "MainViewModel.cs"));
    }

    private async Task GenerateAvaloniaProjectAsync(string moduleDir)
    {
        var uiDir = Path.Combine(moduleDir, $"{_context.ModuleName}.UI.Avalonia");
        Directory.CreateDirectory(uiDir);
        
        await WriteTemplateAsync("Avalonia/UI.Avalonia.csproj.template", 
            Path.Combine(uiDir, $"{_context.ModuleName}.UI.Avalonia.csproj"));
        
        await WriteTemplateAsync("Avalonia/AvaloniaModule.cs.template", 
            Path.Combine(uiDir, $"{_context.ModuleName}AvaloniaModule.cs"));
        
        await WriteTemplateAsync("Avalonia/MainView.axaml.template", 
            Path.Combine(uiDir, "MainView.axaml"));
        
        await WriteTemplateAsync("Avalonia/MainView.axaml.cs.template", 
            Path.Combine(uiDir, "MainView.axaml.cs"));
    }

    private async Task GenerateBlazorProjectAsync(string moduleDir)
    {
        var uiDir = Path.Combine(moduleDir, $"{_context.ModuleName}.UI.Blazor");
        Directory.CreateDirectory(uiDir);
        
        await WriteTemplateAsync("Blazor/UI.Blazor.csproj.template", 
            Path.Combine(uiDir, $"{_context.ModuleName}.UI.Blazor.csproj"));
        
        await WriteTemplateAsync("Blazor/BlazorModule.cs.template", 
            Path.Combine(uiDir, $"{_context.ModuleName}BlazorModule.cs"));
        
        await WriteTemplateAsync("Blazor/_Imports.razor.template", 
            Path.Combine(uiDir, "_Imports.razor"));
        
        await WriteTemplateAsync("Blazor/MainView.razor.template", 
            Path.Combine(uiDir, "MainView.razor"));
    }

    private async Task GenerateManifestAsync(string moduleDir)
    {
        var templateName = _context.TargetHost == TargetHostType.Avalonia
            ? "extension.vsixmanifest.avalonia.template"
            : "extension.vsixmanifest.blazor.template";
        
        await WriteTemplateAsync(templateName, 
            Path.Combine(moduleDir, "extension.vsixmanifest"));
    }

    private async Task WriteTemplateAsync(string templateName, string outputPath)
    {
        var template = await LoadTemplateAsync(templateName);
        var content = ReplaceVariables(template);
        
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
    }

    private async Task<string> LoadTemplateAsync(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Modulus.Cli.Templates.{templateName.Replace('/', '.')}";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Fallback: read from file system (for development)
            var basePath = Path.GetDirectoryName(assembly.Location) ?? ".";
            var filePath = Path.Combine(basePath, "Templates", templateName);
            
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            
            throw new FileNotFoundException($"Template not found: {templateName}");
        }
        
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private string ReplaceVariables(string template)
    {
        return template
            .Replace("{{ModuleName}}", _context.ModuleName)
            .Replace("{{ModuleNameLower}}", _context.ModuleNameLower)
            .Replace("{{DisplayName}}", _context.DisplayName)
            .Replace("{{Description}}", _context.Description)
            .Replace("{{Publisher}}", _context.Publisher)
            .Replace("{{ModuleId}}", _context.ModuleId)
            .Replace("{{Icon}}", _context.Icon)
            .Replace("{{Order}}", _context.Order.ToString());
    }
}

