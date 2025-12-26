using System.Reflection;
using System.Text;

namespace Modulus.Cli.Templates;

/// <summary>
/// Engine for generating host app projects from templates.
/// </summary>
public sealed class HostAppTemplateEngine
{
    private readonly HostAppTemplateContext _context;

    public HostAppTemplateEngine(HostAppTemplateContext context)
    {
        _context = context;
    }

    public async Task GenerateAsync(string outputPath)
    {
        var appDir = Path.Combine(outputPath, _context.AppName);
        Directory.CreateDirectory(appDir);

        await GenerateDirectoryBuildPropsAsync(appDir);

        var hostProjectName = _context.TargetHost == TargetHostType.Avalonia
            ? $"{_context.AppName}.Host.Avalonia"
            : $"{_context.AppName}.Host.Blazor";

        var hostDir = Path.Combine(appDir, hostProjectName);
        Directory.CreateDirectory(hostDir);

        if (_context.TargetHost == TargetHostType.Avalonia)
        {
            await WriteTemplateAsync("HostApp/Avalonia/Host.Avalonia.csproj.template", Path.Combine(hostDir, $"{hostProjectName}.csproj"));
            await WriteTemplateAsync("HostApp/Avalonia/Program.cs.template", Path.Combine(hostDir, "Program.cs"));
            await WriteTemplateAsync("HostApp/Avalonia/App.axaml.template", Path.Combine(hostDir, "App.axaml"));
            await WriteTemplateAsync("HostApp/Avalonia/App.axaml.cs.template", Path.Combine(hostDir, "App.axaml.cs"));
            await WriteTemplateAsync("HostApp/Avalonia/MainWindow.axaml.template", Path.Combine(hostDir, "MainWindow.axaml"));
            await WriteTemplateAsync("HostApp/Avalonia/MainWindow.axaml.cs.template", Path.Combine(hostDir, "MainWindow.axaml.cs"));
        }
        else
        {
            await WriteTemplateAsync("HostApp/Blazor/Host.Blazor.csproj.template", Path.Combine(hostDir, $"{hostProjectName}.csproj"));
            await WriteTemplateAsync("HostApp/Blazor/MauiProgram.cs.template", Path.Combine(hostDir, "MauiProgram.cs"));
            await WriteTemplateAsync("HostApp/Blazor/App.xaml.template", Path.Combine(hostDir, "App.xaml"));
            await WriteTemplateAsync("HostApp/Blazor/App.xaml.cs.template", Path.Combine(hostDir, "App.xaml.cs"));
            await WriteTemplateAsync("HostApp/Blazor/MainPage.xaml.template", Path.Combine(hostDir, "MainPage.xaml"));
            await WriteTemplateAsync("HostApp/Blazor/MainPage.xaml.cs.template", Path.Combine(hostDir, "MainPage.xaml.cs"));
            await WriteTemplateAsync("HostApp/Blazor/Components/App.razor.template", Path.Combine(hostDir, "Components", "App.razor"));
            await WriteTemplateAsync("HostApp/Blazor/Components/Routes.razor.template", Path.Combine(hostDir, "Components", "Routes.razor"));
            await WriteTemplateAsync("HostApp/Blazor/wwwroot/index.html.template", Path.Combine(hostDir, "wwwroot", "index.html"));

            // Windows platform entrypoint (required for MAUI Windows builds)
            await WriteTemplateAsync("HostApp/Blazor/Platforms/Windows/App.xaml.template", Path.Combine(hostDir, "Platforms", "Windows", "App.xaml"));
            await WriteTemplateAsync("HostApp/Blazor/Platforms/Windows/App.xaml.cs.template", Path.Combine(hostDir, "Platforms", "Windows", "App.xaml.cs"));
            await WriteTemplateAsync("HostApp/Blazor/Platforms/Windows/app.manifest.template", Path.Combine(hostDir, "Platforms", "Windows", "app.manifest"));
            await WriteTemplateAsync("HostApp/Blazor/Platforms/Windows/Package.appxmanifest.template", Path.Combine(hostDir, "Platforms", "Windows", "Package.appxmanifest"));
        }

        await WriteTemplateAsync(
            _context.TargetHost == TargetHostType.Avalonia
                ? "HostApp/appsettings.avalonia.json.template"
                : "HostApp/appsettings.blazor.json.template",
            Path.Combine(hostDir, "appsettings.json"));

        await GenerateSolutionAsync(appDir, hostProjectName);
        await GenerateGitIgnoreAsync(appDir);
    }

    private static async Task GenerateDirectoryBuildPropsAsync(string appDir)
    {
        var cliDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        var content = $@"<Project>
  <PropertyGroup>
    <!-- Absolute path to Modulus CLI installation directory (contains Modulus.*.dll). -->
    <ModulusCliLibDir>{EscapeXml(cliDir)}</ModulusCliLibDir>
  </PropertyGroup>
</Project>
";
        await File.WriteAllTextAsync(Path.Combine(appDir, "Directory.Build.props"), content, Encoding.UTF8);
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private async Task GenerateSolutionAsync(string appDir, string hostProjectName)
    {
        var hostGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
        var slnGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();

        var sln = $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{hostProjectName}"", ""{hostProjectName}\{hostProjectName}.csproj"", ""{hostGuid}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{hostGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{hostGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{hostGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{hostGuid}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {slnGuid}
	EndGlobalSection
EndGlobal
";

        await File.WriteAllTextAsync(Path.Combine(appDir, $"{_context.AppName}.sln"), sln, Encoding.UTF8);
    }

    private static async Task GenerateGitIgnoreAsync(string appDir)
    {
        var gitignore = @"## .NET
bin/
obj/
*.user
*.suo
*.cache
*.log

## IDE
.vs/
.idea/
*.swp
*~

## Build
artifacts/
publish/
";
        await File.WriteAllTextAsync(Path.Combine(appDir, ".gitignore"), gitignore, Encoding.UTF8);
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
            .Replace("{{AppName}}", _context.AppName)
            .Replace("{{AppNameLower}}", _context.AppNameLower);
    }
}


