using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Core.Paths;
using Modulus.Sdk;

namespace Modulus.Cli.Commands.Handlers;

/// <summary>
/// Handles module installation logic. Can be used directly for testing.
/// </summary>
public class InstallHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly string? _modulesDirectory;
    
    public InstallHandler(IServiceProvider serviceProvider, string? modulesDirectory = null)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<InstallHandler>>();
        _modulesDirectory = modulesDirectory;
    }
    
    /// <summary>
    /// Result of an install operation.
    /// </summary>
    public record InstallResult(bool Success, string Message, string? ModuleId = null);
    
    /// <summary>
    /// Install a module from a .modpkg file or directory.
    /// </summary>
    public async Task<InstallResult> ExecuteAsync(string source, bool force = false, TextWriter? output = null)
    {
        output ??= Console.Out;
        
        try
        {
            string moduleDir;
            string? tempDir = null;

            // Determine source type
            if (source.EndsWith(".modpkg", StringComparison.OrdinalIgnoreCase) && File.Exists(source))
            {
                // Extract .modpkg to temp directory
                await output.WriteLineAsync($"Extracting package: {Path.GetFileName(source)}");
                tempDir = Path.Combine(Path.GetTempPath(), $"modulus-install-{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    ZipFile.ExtractToDirectory(source, tempDir);
                    moduleDir = tempDir;
                }
                catch (Exception ex)
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                    return new InstallResult(false, $"Failed to extract package: {ex.Message}");
                }
            }
            else if (Directory.Exists(source))
            {
                moduleDir = source;
            }
            else
            {
                return new InstallResult(false, $"Source not found: {source}");
            }

            try
            {
                // Read manifest
                var manifestPath = Path.Combine(moduleDir, "extension.vsixmanifest");
                if (!File.Exists(manifestPath))
                {
                    return new InstallResult(false, "extension.vsixmanifest not found in package");
                }

                var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath);
                if (manifest == null)
                {
                    return new InstallResult(false, "Failed to read manifest");
                }

                var identity = manifest.Metadata.Identity;
                await output.WriteLineAsync($"Installing: {manifest.Metadata.DisplayName} v{identity.Version}");
                await output.WriteLineAsync($"  Publisher: {identity.Publisher}");
                await output.WriteLineAsync($"  ID: {identity.Id}");

                // Determine installation directory
                var modulesRoot = _modulesDirectory ?? Path.Combine(LocalStorage.GetUserRoot(), "Modules");
                var targetDir = Path.Combine(modulesRoot, identity.Id);

                // Check if already exists
                if (Directory.Exists(targetDir))
                {
                    if (!force)
                    {
                        return new InstallResult(false, $"Module {identity.Id} is already installed. Use --force to overwrite.");
                    }
                    
                    await output.WriteLineAsync("Removing existing installation...");
                    Directory.Delete(targetDir, true);
                }

                // Copy files to target directory
                await output.WriteLineAsync($"Copying files to: {targetDir}");
                Directory.CreateDirectory(targetDir);
                CopyDirectory(moduleDir, targetDir);

                // Register in database
                await output.WriteLineAsync("Registering module in database...");
                var installerService = _serviceProvider.GetRequiredService<IModuleInstallerService>();
                await installerService.InstallFromPathAsync(targetDir, isSystem: false, hostType: null);

                await output.WriteLineAsync();
                await output.WriteLineAsync($"✓ Module '{manifest.Metadata.DisplayName}' installed successfully!");
                await output.WriteLineAsync($"  Location: {targetDir}");

                return new InstallResult(true, "Module installed successfully", identity.Id);
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
            _logger.LogError(ex, "Installation failed");
            return new InstallResult(false, ex.Message);
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

