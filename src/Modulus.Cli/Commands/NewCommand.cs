using System.CommandLine;
using System.CommandLine.Parsing;
using Modulus.Cli.Templates;

namespace Modulus.Cli.Commands;

/// <summary>
/// New command: modulus new [<template>] -n <name> [options]
/// </summary>
public static class NewCommand
{
    private const string DefaultTemplate = "module-avalonia";

    private static readonly (string Name, string Description)[] Templates =
    [
        ("module-avalonia", "Modulus module (Avalonia)"),
        ("module-blazor", "Modulus module (Blazor)"),
    ];

    public static Command Create()
    {
        var templateArg = new Argument<string?>("template")
        {
            Description = "Template name (module-avalonia or module-blazor)",
            Arity = ArgumentArity.ZeroOrOne
        };

        var nameOption = new Option<string?>("--name", "-n") { Description = "Module name (PascalCase, e.g., MyModule)" };
        var outputOption = new Option<string?>("--output", "-o") { Description = "Output directory (default: current directory)" };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Overwrite existing directory without prompting" };
        var listOption = new Option<bool>("--list") { Description = "List available templates and exit" };

        var command = new Command("new", "Create a new Modulus module project");
        command.Arguments.Add(templateArg);
        command.Options.Add(nameOption);
        command.Options.Add(outputOption);
        command.Options.Add(forceOption);
        command.Options.Add(listOption);

        // Validate template when provided
        templateArg.Validators.Add(argumentResult =>
        {
            var template = argumentResult.GetValueOrDefault<string?>();
            if (string.IsNullOrWhiteSpace(template))
            {
                return;
            }

            if (!Templates.Any(t => string.Equals(t.Name, template, StringComparison.OrdinalIgnoreCase)))
            {
                argumentResult.AddError($"Unknown template '{template}'. Use --list to see available templates.");
            }
        });

        // Validate: --name is required unless --list is specified
        command.Validators.Add(commandResult =>
        {
            var isList = commandResult.GetValue(listOption);
            var name = commandResult.GetValue(nameOption);

            if (!isList && string.IsNullOrWhiteSpace(name))
            {
                commandResult.AddError("Missing required option --name. Use --list to list templates.");
            }
        });

        // Validate module name format when provided
        nameOption.Validators.Add(optionResult =>
        {
            var name = optionResult.GetValueOrDefault<string?>();
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!IsValidModuleName(name))
            {
                optionResult.AddError("Module name must be PascalCase (e.g., MyModule)");
            }
        });

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var template = parseResult.GetValue(templateArg);
            var name = parseResult.GetValue(nameOption);
            var output = parseResult.GetValue(outputOption);
            var force = parseResult.GetValue(forceOption);
            var list = parseResult.GetValue(listOption);

            await HandleAsync(template, name, output, force, list);
        });

        return command;
    }

    private static async Task HandleAsync(
        string? template,
        string? name,
        string? output,
        bool force,
        bool list)
    {
        if (list)
        {
            PrintTemplates();
            return;
        }

        // Command validator guarantees name is present and valid unless --list is specified.
        name ??= "";

        // Determine template/target
        var effectiveTemplate = string.IsNullOrWhiteSpace(template) ? DefaultTemplate : template;
        TargetHostType targetHost;
        switch (effectiveTemplate)
        {
            case "module-avalonia":
                targetHost = TargetHostType.Avalonia;
                break;
            case "module-blazor":
                targetHost = TargetHostType.Blazor;
                break;
            default:
                // Should be unreachable due to FromAmong validator.
                Console.Error.WriteLine($"Error: Unknown template '{effectiveTemplate}'.");
                return;
        }

        // Resolve output directory
        output = string.IsNullOrWhiteSpace(output) ? Directory.GetCurrentDirectory() : output;
        output = Path.GetFullPath(output);

        // Check output directory
        var moduleDir = Path.Combine(output, name);
        if (Directory.Exists(moduleDir) && Directory.GetFileSystemEntries(moduleDir).Length > 0)
        {
            if (!force)
            {
                if (Console.IsInputRedirected)
                {
                    Console.Error.WriteLine($"Error: Directory '{moduleDir}' already exists. Use --force to overwrite in non-interactive mode.");
                    return;
                }

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
            DisplayName = name,
            Description = "A Modulus module.",
            Publisher = "Modulus Team",
            ModuleId = Guid.NewGuid().ToString("D"),
            Icon = "Folder",
            Order = 100,
            TargetHost = targetHost
        };

        // Generate project
        Console.WriteLine();
        Console.WriteLine($"Creating Modulus module '{name}' ({effectiveTemplate})...");
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

    private static void PrintTemplates()
    {
        Console.WriteLine("Available templates:");
        foreach (var (name, description) in Templates)
        {
            Console.WriteLine($"  {name,-15} {description}");
        }
    }
}
