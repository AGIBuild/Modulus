using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Cli.Services;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Core.Paths;
using Modulus.Sdk;

namespace Modulus.Cli.Commands;

/// <summary>
/// Install command: modulus install &lt;source&gt;
/// </summary>
public static class InstallCommand
{
    public static Command Create()
    {
        var sourceArg = new Argument<string>("source")
        {
            Description = "Path to .modpkg file or module directory"
        };
        
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Overwrite existing installation without prompting"
        };
        
        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed output"
        };

        var command = new Command("install", "Install a module from a .modpkg file or directory");
        command.Add(sourceArg);
        command.Add(forceOption);
        command.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var source = parseResult.GetValue(sourceArg)!;
            var force = parseResult.GetValue(forceOption);
            var verbose = parseResult.GetValue(verboseOption);
            await HandleAsync(source, force, verbose);
        });
        
        return command;
    }

    private static async Task HandleAsync(string source, bool force, bool verbose)
    {
        using var provider = CliServiceProvider.Build(verbose);
        var logger = provider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Ensure database is migrated
            await CliServiceProvider.EnsureMigratedAsync(provider);

            string moduleDir;
            string? tempDir = null;

            // Determine source type
            if (source.EndsWith(".modpkg", StringComparison.OrdinalIgnoreCase) && File.Exists(source))
            {
                // Extract .modpkg to temp directory
                Console.WriteLine($"Extracting package: {Path.GetFileName(source)}");
                tempDir = Path.Combine(Path.GetTempPath(), $"modulus-install-{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    ZipFile.ExtractToDirectory(source, tempDir);
                    moduleDir = tempDir;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: Failed to extract package: {ex.Message}");
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                    return;
                }
            }
            else if (Directory.Exists(source))
            {
                moduleDir = source;
            }
            else
            {
                Console.WriteLine($"Error: Source not found: {source}");
                return;
            }

            try
            {
                // Read manifest
                var manifestPath = Path.Combine(moduleDir, "extension.vsixmanifest");
                if (!File.Exists(manifestPath))
                {
                    Console.WriteLine("Error: extension.vsixmanifest not found in package");
                    return;
                }

                var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath);
                if (manifest == null)
                {
                    Console.WriteLine("Error: Failed to read manifest");
                    return;
                }

                var identity = manifest.Metadata.Identity;
                Console.WriteLine($"Installing: {manifest.Metadata.DisplayName} v{identity.Version}");
                Console.WriteLine($"  Publisher: {identity.Publisher}");
                Console.WriteLine($"  ID: {identity.Id}");

                // Determine installation directory
                var modulesRoot = Path.Combine(LocalStorage.GetUserRoot(), "Modules");
                var targetDir = Path.Combine(modulesRoot, identity.Id);

                // Check if already exists
                if (Directory.Exists(targetDir))
                {
                    if (!force)
                    {
                        Console.Write($"Module {identity.Id} is already installed. Overwrite? [y/N]: ");
                        var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                        if (response != "y" && response != "yes")
                        {
                            Console.WriteLine("Installation cancelled.");
                            return;
                        }
                    }
                    
                    Console.WriteLine($"Removing existing installation...");
                    Directory.Delete(targetDir, true);
                }

                // Copy files to target directory
                Console.WriteLine($"Copying files to: {targetDir}");
                Directory.CreateDirectory(targetDir);
                CopyDirectory(moduleDir, targetDir);

                // Register in database
                Console.WriteLine("Registering module in database...");
                var installerService = provider.GetRequiredService<IModuleInstallerService>();
                await installerService.InstallFromPathAsync(targetDir, isSystem: false, hostType: null);

                Console.WriteLine();
                Console.WriteLine($"✓ Module '{manifest.Metadata.DisplayName}' installed successfully!");
                Console.WriteLine($"  Location: {targetDir}");
                Console.WriteLine();
                Console.WriteLine("Note: Restart the Modulus host application to load the module.");
            }
            finally
            {
                // Clean up temp directory
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Installation failed");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, targetSubDir);
        }
    }
}
