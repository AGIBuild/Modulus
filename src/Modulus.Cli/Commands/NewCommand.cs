using System.CommandLine;
using Modulus.Cli.Templates;

namespace Modulus.Cli.Commands;

/// <summary>
/// New command: modulus new &lt;name&gt; [options]
/// </summary>
public static class NewCommand
{
    public static Command Create()
    {
        var nameArg = new Argument<string>("name") { Description = "Module name (PascalCase, e.g., MyModule)" };

        var targetOption = new Option<string?>("--target", "-t") { Description = "Target host: avalonia or blazor" };
        var displayNameOption = new Option<string?>("--display-name", "-d") { Description = "Display name shown in menus" };
        var descriptionOption = new Option<string?>("--description") { Description = "Module description" };
        var publisherOption = new Option<string?>("--publisher", "-p") { Description = "Publisher name" };
        var iconOption = new Option<string?>("--icon", "-i") { Description = "Menu icon (e.g., Apps, Terminal, Settings)" };
        var orderOption = new Option<int?>("--order", "-o") { Description = "Menu order (default: 100)" };
        var outputOption = new Option<string?>("--output") { Description = "Output directory (default: current directory)" };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Overwrite existing directory without prompting" };

        var command = new Command("new", "Create a new Modulus module project");
        command.Arguments.Add(nameArg);
        command.Options.Add(targetOption);
        command.Options.Add(displayNameOption);
        command.Options.Add(descriptionOption);
        command.Options.Add(publisherOption);
        command.Options.Add(iconOption);
        command.Options.Add(orderOption);
        command.Options.Add(outputOption);
        command.Options.Add(forceOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var target = parseResult.GetValue(targetOption);
            var displayName = parseResult.GetValue(displayNameOption);
            var description = parseResult.GetValue(descriptionOption);
            var publisher = parseResult.GetValue(publisherOption);
            var icon = parseResult.GetValue(iconOption);
            var order = parseResult.GetValue(orderOption);
            var output = parseResult.GetValue(outputOption);
            var force = parseResult.GetValue(forceOption);

            await HandleAsync(name, target, displayName, description, publisher, icon, order, output, force);
        });

        return command;
    }

    private static async Task HandleAsync(
        string name,
        string? target,
        string? displayName,
        string? description,
        string? publisher,
        string? icon,
        int? order,
        string? output,
        bool force)
    {
        // Validate module name
        if (!IsValidModuleName(name))
        {
            Console.WriteLine("Error: Module name must be PascalCase (e.g., MyModule)");
            return;
        }

        // Determine if we need interactive mode
        var isInteractive = !Console.IsInputRedirected && string.IsNullOrEmpty(target);

        // Get target host
        TargetHostType targetHost;
        if (!string.IsNullOrEmpty(target))
        {
            if (!TryParseTarget(target, out targetHost))
            {
                Console.WriteLine("Error: Invalid target. Use 'avalonia' or 'blazor'.");
                return;
            }
        }
        else if (isInteractive)
        {
            targetHost = PromptForTarget();
        }
        else
        {
            Console.WriteLine("Error: --target is required in non-interactive mode.");
            return;
        }

        // Collect other options with defaults or prompts
        displayName ??= isInteractive ? PromptWithDefault("Display name", name) : name;
        description ??= isInteractive ? PromptWithDefault("Description", "A Modulus module.") : "A Modulus module.";
        publisher ??= isInteractive ? PromptWithDefault("Publisher", "Modulus Team") : "Modulus Team";
        icon ??= isInteractive ? PromptWithDefault("Icon", "Folder") : "Folder";
        order ??= isInteractive ? int.Parse(PromptWithDefault("Menu order", "100")) : 100;
        output ??= Directory.GetCurrentDirectory();

        // Check output directory
        var moduleDir = Path.Combine(output, name);
        if (Directory.Exists(moduleDir) && Directory.GetFileSystemEntries(moduleDir).Length > 0)
        {
            if (!force)
            {
                Console.Write($"Directory '{moduleDir}' already exists. Overwrite? [y/N]: ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (response != "y" && response != "yes")
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }
            Directory.Delete(moduleDir, true);
        }

        // Create context
        var context = new ModuleTemplateContext
        {
            ModuleName = name,
            DisplayName = displayName,
            Description = description,
            Publisher = publisher,
            ModuleId = Guid.NewGuid().ToString("D"),
            Icon = icon,
            Order = order.Value,
            TargetHost = targetHost
        };

        // Generate project
        Console.WriteLine();
        Console.WriteLine($"Creating Modulus module '{name}' ({targetHost})...");
        Console.WriteLine();

        var engine = new TemplateEngine(context);
        await engine.GenerateAsync(output);

        Console.WriteLine($"✓ Created {name}.sln");
        Console.WriteLine($"✓ Created {name}.Core/");
        Console.WriteLine($"✓ Created {name}.UI.{targetHost}/");
        Console.WriteLine($"✓ Created extension.vsixmanifest");
        Console.WriteLine($"✓ Created .gitignore");
        Console.WriteLine();
        Console.WriteLine($"Module created successfully at: {moduleDir}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  1. Open {name}.sln in Visual Studio / Rider");
        Console.WriteLine($"  2. Or: cd {name} && dotnet build");
        Console.WriteLine($"  3. Copy output to Modulus Modules directory");
    }

    private static bool IsValidModuleName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsUpper(name[0])) return false;
        return name.All(c => char.IsLetterOrDigit(c));
    }

    private static bool TryParseTarget(string target, out TargetHostType result)
    {
        result = target.ToLowerInvariant() switch
        {
            "avalonia" => TargetHostType.Avalonia,
            "blazor" => TargetHostType.Blazor,
            _ => default
        };
        return target.ToLowerInvariant() is "avalonia" or "blazor";
    }

    private static TargetHostType PromptForTarget()
    {
        Console.WriteLine("Select target host:");
        Console.WriteLine("  1. Avalonia (Desktop)");
        Console.WriteLine("  2. Blazor (Web)");
        Console.Write("Enter choice [1]: ");

        var input = Console.ReadLine()?.Trim();
        return input switch
        {
            "2" => TargetHostType.Blazor,
            _ => TargetHostType.Avalonia
        };
    }

    private static string PromptWithDefault(string prompt, string defaultValue)
    {
        Console.Write($"{prompt} [{defaultValue}]: ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }
}
