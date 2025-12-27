using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core;
using Modulus.Core.Data;
using Modulus.Core.Installation;
using Modulus.Core.Paths;
using Modulus.Core.Runtime;
using Modulus.HostSdk.Abstractions;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;

namespace Modulus.HostSdk.Runtime;

/// <summary>
/// Host SDK builder for composing a Modulus host application.
/// </summary>
public sealed class ModulusHostSdkBuilder
{
    private readonly List<HostModuleDirectory> _moduleDirectories = new();

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    public ModulusHostSdkOptions Options { get; }

    public ModulusHostSdkBuilder(IServiceCollection services, IConfiguration configuration, ModulusHostSdkOptions options)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Options = options ?? throw new ArgumentNullException(nameof(options));

        if (options.ModuleDirectories.Count > 0)
        {
            _moduleDirectories.AddRange(options.ModuleDirectories);
        }
    }

    /// <summary>
    /// Adds default module directories:
    /// - {AppBaseDir}/Modules (system)
    /// - {UserRoot}/Modules (user)
    /// </summary>
    public ModulusHostSdkBuilder AddDefaultModuleDirectories()
    {
        var appModules = Path.Combine(AppContext.BaseDirectory, "Modules");
        _moduleDirectories.Add(new HostModuleDirectory(appModules, IsSystem: true));

        // Canonical user modules directory: align with Modulus.Core.Paths.LocalStorage (Windows: %APPDATA%/Modulus; others: $HOME/.modulus).
        var userModules = Path.Combine(LocalStorage.GetUserRoot(), "Modules");
        _moduleDirectories.Add(new HostModuleDirectory(userModules, IsSystem: false));

        return this;
    }

    public ModulusHostSdkBuilder AddModuleDirectory(string path, bool isSystem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _moduleDirectories.Add(new HostModuleDirectory(path, isSystem));
        return this;
    }

    /// <summary>
    /// Registers the default database and runtime services required by Modulus hosts.
    /// </summary>
    public ModulusHostSdkBuilder AddDefaultRuntimeServices()
    {
        // Database
        Services.AddModulusDatabase(Options.DatabasePath);

        // Repositories & installers
        Services.AddScoped<IModuleRepository, ModuleRepository>();
        Services.AddScoped<IMenuRepository, MenuRepository>();
        Services.AddScoped<IPendingCleanupRepository, PendingCleanupRepository>();
        Services.AddSingleton<IModuleCleanupService, ModuleCleanupService>();
        Services.AddScoped<IModuleInstallerService, ModuleInstallerService>();
        Services.AddScoped<SystemModuleInstaller>();
        Services.AddScoped<ModuleIntegrityChecker>();
        Services.AddSingleton<ILazyModuleLoader, LazyModuleLoader>();

        return this;
    }

    public async Task<IModulusApplication> BuildAsync<TStartupModule>(ILoggerFactory loggerFactory)
        where TStartupModule : IModule, new()
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        // Filter directories that actually exist (and de-duplicate paths) to avoid noisy warnings and double-scanning.
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var dirs = _moduleDirectories
            .Select(d => new { Dir = d, NormalizedPath = NormalizeDirectoryPath(d.Path) })
            .GroupBy(x => x.NormalizedPath, pathComparer)
            .Select(g => g.OrderByDescending(x => x.Dir.IsSystem).First().Dir)
            .Where(d => Directory.Exists(d.Path))
            .Select(d => new ModuleDirectory(d.Path, d.IsSystem))
            .ToList();

        var app = await ModulusApplicationFactory.CreateAsync<TStartupModule>(
            Services,
            dirs,
            Options.HostId,
            Options.DatabasePath,
            Configuration,
            loggerFactory,
            Options.HostVersion).ConfigureAwait(false);

        return app;
    }

    private static string NormalizeDirectoryPath(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}


