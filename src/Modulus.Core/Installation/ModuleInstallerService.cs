using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Installation;

public class ModuleInstallerService : IModuleInstallerService
{
    private readonly IModuleRepository _moduleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly ISharedAssemblyCatalog _sharedCatalog;
    private readonly IManifestValidator _manifestValidator;
    private readonly ModuleMetadataScanner _scanner;
    private readonly ILogger<ModuleInstallerService> _logger;

    public ModuleInstallerService(
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        ISharedAssemblyCatalog sharedCatalog,
        IManifestValidator manifestValidator,
        ILogger<ModuleInstallerService> logger)
    {
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _sharedCatalog = sharedCatalog;
        _manifestValidator = manifestValidator;
        _logger = logger;
        _scanner = new ModuleMetadataScanner(logger);
    }

    public async Task InstallFromPathAsync(string packagePath, bool isSystem = false, string? hostType = null, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(packagePath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Manifest not found", manifestPath);
        }

        var manifest = await ManifestReader.ReadFromFileAsync(manifestPath, cancellationToken);
        if (manifest == null)
        {
            throw new InvalidOperationException($"Failed to read manifest from {manifestPath}");
        }

        // Validate manifest with host-aware validation
        var validationResult = await _manifestValidator.ValidateAsync(packagePath, manifestPath, manifest, hostType, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _logger.LogError("Manifest validation error for {ModuleId}: {Error}", manifest.Id, error);
            }
        }

        // Isolation context for inspection
        // Note: passing null for logger to avoid context leaking into logger scope if possible, 
        // though our ModuleLoadContext holds a logger reference.
        var inspectionContext = new ModuleLoadContext(manifest.Id, packagePath, _sharedCatalog, _logger);
        
        try
        {
            var loadedAssemblies = new List<Assembly>();

            // Load Core Assemblies
            foreach (var asmName in manifest.CoreAssemblies)
            {
                var assembly = LoadAssembly(inspectionContext, asmName);
                if (assembly != null) loadedAssemblies.Add(assembly);
            }

            // Load UI Assemblies (only for current host if specified)
            if (manifest.UiAssemblies != null)
            {
                if (hostType != null && manifest.UiAssemblies.TryGetValue(hostType, out var hostAssemblies))
                {
                    // Load only for current host
                    foreach (var asmName in hostAssemblies)
                    {
                        var assembly = LoadAssembly(inspectionContext, asmName);
                        if (assembly != null) loadedAssemblies.Add(assembly);
                    }
                }
                else if (hostType == null)
                {
                    // No host specified - load all (for backward compatibility)
                    foreach (var hostGroup in manifest.UiAssemblies.Values)
                    {
                        foreach (var asmName in hostGroup)
                        {
                            var assembly = LoadAssembly(inspectionContext, asmName);
                            if (assembly != null) loadedAssemblies.Add(assembly);
                        }
                    }
                }
            }

            // Find IModule implementation to verify validity and extract metadata
            var moduleType = loadedAssemblies
                .SelectMany(a => SafeGetTypes(a))
                .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            if (moduleType == null)
            {
                _logger.LogWarning("No IModule implementation found in {ModuleId}.", manifest.Id);
            }

            // Collect menu metadata first to decide module-level location
            var menuMetas = new List<ModuleMenuMetadata>();

            foreach (var assembly in loadedAssemblies)
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    // Avalonia
                    menuMetas.AddRange(_scanner.ScanAvaloniaMenus(type));

                    // Blazor
                    menuMetas.AddRange(_scanner.ScanBlazorMenus(type));
                }
            }

            var requestedBottom = menuMetas.Any(m => m.Location == MenuLocation.Bottom);
            var moduleLocation = (isSystem && requestedBottom) ? MenuLocation.Bottom : MenuLocation.Main;

            if (!isSystem && requestedBottom)
            {
                _logger.LogWarning("Module {ModuleId} requested Bottom menu location but is not system-managed. Forcing to Main.", manifest.Id);
            }

            // Prepare Entities
            // Set state based on validation result
            var moduleState = validationResult.IsValid
                ? Modulus.Infrastructure.Data.Models.ModuleState.Ready
                : Modulus.Infrastructure.Data.Models.ModuleState.Incompatible;
            
            var validationErrors = validationResult.IsValid
                ? null
                : JsonSerializer.Serialize(validationResult.Errors);
            
            var moduleEntity = new ModuleEntity
            {
                Id = manifest.Id,
                Name = manifest.DisplayName ?? manifest.Id,
                Version = manifest.Version,
                Description = manifest.Description,
                Author = manifest.Author,
                Website = manifest.Website,
                EntryComponent = manifest.EntryComponent,
                Path = Path.GetRelativePath(Directory.GetCurrentDirectory(), manifestPath), // Store relative path
                IsSystem = isSystem,
                IsEnabled = validationResult.IsValid, // Only enable if validation passes
                MenuLocation = moduleLocation,
                State = moduleState,
                ValidationErrors = validationErrors
            };

            var menuEntities = new List<MenuEntity>();

            foreach (var meta in menuMetas)
            {
                menuEntities.Add(MapMenu(meta, manifest.Id, moduleLocation));
            }

            // Persist
            _logger.LogInformation("Installing module {ModuleId} v{Version} to database...", manifest.Id, manifest.Version);
            
            await _moduleRepository.UpsertAsync(moduleEntity, cancellationToken);
            await _menuRepository.ReplaceModuleMenusAsync(manifest.Id, menuEntities, cancellationToken);
            
            _logger.LogInformation("Module {ModuleId} installed successfully.", manifest.Id);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install module {ModuleId} from {Path}", manifest.Id, packagePath);
            throw;
        }
        finally
        {
            inspectionContext.Unload();
        }
    }

    public Task RegisterDevelopmentModuleAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(manifestPath);
        if (dir == null) throw new ArgumentException("Invalid manifest path");
        
        return InstallFromPathAsync(dir, isSystem: false, hostType: null, cancellationToken);
    }

    private Assembly? LoadAssembly(ModuleLoadContext context, string assemblyName)
    {
        try
        {
            // Remove .dll extension if present - AssemblyName expects just the name
            var name = assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? assemblyName[..^4]
                : assemblyName;
            return context.LoadFromAssemblyName(new AssemblyName(name));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load assembly {AssemblyName} for inspection.", assemblyName);
            return null;
        }
    }

    private IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }

    private MenuEntity MapMenu(ModuleMenuMetadata meta, string moduleId, MenuLocation moduleLocation)
    {
        return new MenuEntity
        {
            Id = $"{moduleId}.{meta.Id}", // Ensure uniqueness
            ModuleId = moduleId,
            DisplayName = meta.DisplayName,
            Icon = meta.Icon.ToString(), // Store enum as string
            Route = !string.IsNullOrEmpty(meta.Route) ? meta.Route : meta.ViewModelType,
            Location = moduleLocation,
            Order = meta.Order,
            ParentId = null // TODO: Support nesting
        };
    }
}

