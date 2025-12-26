using Modulus.Cli.Services;
using Modulus.Cli.Templates;

namespace Modulus.Cli.Commands.Handlers;

/// <summary>
/// Handles project generation for <c>modulus new</c>.
/// </summary>
public sealed class NewHandler
{
    private const string DefaultTemplate = "module-avalonia";

    private static readonly (string Name, string Description)[] Templates =
    [
        ("avaloniaapp", "Modulus host app (Avalonia)"),
        ("blazorapp", "Modulus host app (Blazor Hybrid / MAUI)"),
        ("module-avalonia", "Modulus module (Avalonia)"),
        ("module-blazor", "Modulus module (Blazor)"),
    ];

    private readonly ICliConsole _console;

    public NewHandler(ICliConsole console)
    {
        _console = console;
    }

    public async Task<int> ExecuteAsync(
        string? template,
        string? name,
        string? output,
        bool force,
        bool list,
        CancellationToken cancellationToken)
    {
        if (list)
        {
            await PrintTemplatesAsync();
            return 0;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            await _console.Error.WriteLineAsync("Error: Missing required option --name. Use --list to list templates.");
            return 1;
        }

        // Determine template/target
        var effectiveTemplate = string.IsNullOrWhiteSpace(template) ? DefaultTemplate : template.Trim();
        TargetHostType targetHost;
        var isHostApp = false;

        switch (effectiveTemplate)
        {
            case "avaloniaapp":
                targetHost = TargetHostType.Avalonia;
                isHostApp = true;
                break;
            case "blazorapp":
                targetHost = TargetHostType.Blazor;
                isHostApp = true;
                break;
            case "module-avalonia":
                targetHost = TargetHostType.Avalonia;
                break;
            case "module-blazor":
                targetHost = TargetHostType.Blazor;
                break;
            default:
                await _console.Error.WriteLineAsync($"Error: Unknown template '{effectiveTemplate}'. Use --list to see available templates.");
                return 1;
        }

        // Resolve output directory
        var effectiveOutput = string.IsNullOrWhiteSpace(output) ? Directory.GetCurrentDirectory() : output;
        effectiveOutput = Path.GetFullPath(effectiveOutput);

        // Check output directory
        var moduleDir = Path.Combine(effectiveOutput, name);
        if (Directory.Exists(moduleDir) && Directory.GetFileSystemEntries(moduleDir).Length > 0)
        {
            if (!force)
            {
                if (_console.IsInputRedirected)
                {
                    await _console.Error.WriteLineAsync(
                        $"Error: Directory '{moduleDir}' already exists. Use --force to overwrite in non-interactive mode.");
                    return 1;
                }

                await _console.Out.WriteAsync($"Directory '{moduleDir}' already exists. Overwrite? [y/N]: ");
                var response = (await _console.In.ReadLineAsync())?.Trim().ToLowerInvariant();
                if (response != "y" && response != "yes")
                {
                    await _console.Out.WriteLineAsync("Cancelled.");
                    return 1;
                }
            }

            Directory.Delete(moduleDir, true);
        }

        if (isHostApp)
        {
            var context = new HostAppTemplateContext
            {
                AppName = name,
                TargetHost = targetHost
            };

            await _console.Out.WriteLineAsync();
            await _console.Out.WriteLineAsync($"Creating Modulus host app '{name}' ({effectiveTemplate})...");
            await _console.Out.WriteLineAsync();

            var engine = new HostAppTemplateEngine(context);
            await engine.GenerateAsync(effectiveOutput);

            await _console.Out.WriteLineAsync($"✓ Created {name}.sln");
            await _console.Out.WriteLineAsync($"✓ Created {name}.Host.{targetHost}/");
            await _console.Out.WriteLineAsync("✓ Created appsettings.json");
            await _console.Out.WriteLineAsync("✓ Created .gitignore");
            await _console.Out.WriteLineAsync();
            await _console.Out.WriteLineAsync($"Host app created successfully at: {moduleDir}");
            await _console.Out.WriteLineAsync();
            await _console.Out.WriteLineAsync("Next steps:");
            await _console.Out.WriteLineAsync($"  1. Open {name}.sln in Visual Studio / Rider");
            await _console.Out.WriteLineAsync($"  2. Or: cd {name} && dotnet build");
            await _console.Out.WriteLineAsync("  3. Run the host app and install modules via 'modulus install'");

            return 0;
        }

        var moduleContext = new ModuleTemplateContext
        {
            ModuleName = name,
            DisplayName = name,
            Description = "A Modulus module.",
            Publisher = "Modulus Team",
            ModuleId = Guid.NewGuid().ToString("D"),
            Icon = "Folder",
            Order = 100,
            TargetHost = targetHost
        };

        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync($"Creating Modulus module '{name}' ({effectiveTemplate})...");
        await _console.Out.WriteLineAsync();

        var moduleEngine = new TemplateEngine(moduleContext);
        await moduleEngine.GenerateAsync(effectiveOutput);

        await _console.Out.WriteLineAsync($"✓ Created {name}.sln");
        await _console.Out.WriteLineAsync($"✓ Created {name}.Core/");
        await _console.Out.WriteLineAsync($"✓ Created {name}.UI.{targetHost}/");
        await _console.Out.WriteLineAsync("✓ Created extension.vsixmanifest");
        await _console.Out.WriteLineAsync("✓ Created .gitignore");
        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync($"Module created successfully at: {moduleDir}");
        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync("Next steps:");
        await _console.Out.WriteLineAsync($"  1. Open {name}.sln in Visual Studio / Rider");
        await _console.Out.WriteLineAsync($"  2. Or: cd {name} && dotnet build");
        await _console.Out.WriteLineAsync("  3. Copy output to Modulus Modules directory");

        return 0;
    }

    private async Task PrintTemplatesAsync()
    {
        await _console.Out.WriteLineAsync("Available templates:");
        foreach (var (name, description) in Templates)
        {
            await _console.Out.WriteLineAsync($"  {name,-15} {description}");
        }
    }
}


