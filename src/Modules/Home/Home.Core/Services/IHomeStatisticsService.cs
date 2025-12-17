namespace Modulus.Modules.Home.Services;

/// <summary>
/// Provides statistics about installed and running modules.
/// </summary>
public interface IHomeStatisticsService
{
    /// <summary>
    /// Gets statistics about the current Modulus installation.
    /// </summary>
    Task<HomeStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about the Modulus installation.
/// </summary>
public record HomeStatistics
{
    /// <summary>
    /// Total number of installed modules.
    /// </summary>
    public int InstalledModuleCount { get; init; }
    
    /// <summary>
    /// Number of currently running modules.
    /// </summary>
    public int RunningModuleCount { get; init; }
    
    /// <summary>
    /// Framework version.
    /// </summary>
    public string FrameworkVersion { get; init; } = "";
    
    /// <summary>
    /// Current host type (e.g., Avalonia, Blazor).
    /// </summary>
    public string HostType { get; init; } = "";
    
    /// <summary>
    /// List of running module info.
    /// </summary>
    public IReadOnlyList<ModuleInfo> RunningModules { get; init; } = Array.Empty<ModuleInfo>();
}

/// <summary>
/// Basic information about a module.
/// </summary>
public record ModuleInfo
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Version { get; init; } = "";
    public bool IsRunning { get; init; }
    
    /// <summary>
    /// Navigation key to navigate to this module (ViewModel type name or route).
    /// </summary>
    public string NavigationKey { get; init; } = "";
}


