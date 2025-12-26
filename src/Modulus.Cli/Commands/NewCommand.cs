using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;

namespace Modulus.Cli.Commands;

/// <summary>
/// New command: modulus new [<template>] -n <name> [options]
/// </summary>
public static class NewCommand
{
    private static readonly string[] Templates =
    [
        "avaloniaapp",
        "blazorapp",
        "module-avalonia",
        "module-blazor",
    ];

    public static Command Create(IServiceProvider services)
    {
        var templateArg = new Argument<string?>("template")
        {
            Description = "Template name (avaloniaapp, blazorapp, module-avalonia, module-blazor)",
            Arity = ArgumentArity.ZeroOrOne
        };

        var nameOption = new Option<string?>("--name", "-n") { Description = "Module name (PascalCase, e.g., MyModule)" };
        var outputOption = new Option<string?>("--output", "-o") { Description = "Output directory (default: current directory)" };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Overwrite existing directory without prompting" };
        var listOption = new Option<bool>("--list") { Description = "List available templates and exit" };

        var command = new Command("new", "Create a new Modulus project (module or host app)");
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

            if (!Templates.Any(t => string.Equals(t, template, StringComparison.OrdinalIgnoreCase)))
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

            var handler = services.GetRequiredService<NewHandler>();
            var exitCode = await handler.ExecuteAsync(template, name, output, force, list, cancellationToken);
            return exitCode;
        });

        return command;
    }

    private static bool IsValidModuleName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsUpper(name[0])) return false;
        return name.All(c => char.IsLetterOrDigit(c));
    }
}
