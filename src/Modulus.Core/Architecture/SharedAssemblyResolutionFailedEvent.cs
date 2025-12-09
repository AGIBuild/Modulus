using Microsoft.Extensions.Logging;
using Modulus.Architecture;

namespace Modulus.Core.Architecture;

/// <summary>
/// Event raised when a shared assembly resolution fails.
/// This is used for telemetry and diagnostics reporting.
/// </summary>
public sealed record SharedAssemblyResolutionFailedEvent
{
    /// <summary>
    /// The module that triggered the resolution attempt.
    /// </summary>
    public required string ModuleId { get; init; }
    
    /// <summary>
    /// The assembly name that failed to resolve.
    /// </summary>
    public required string AssemblyName { get; init; }
    
    /// <summary>
    /// The source that requested the assembly as shared.
    /// </summary>
    public required SharedAssemblySource Source { get; init; }
    
    /// <summary>
    /// The declared domain type if known.
    /// </summary>
    public AssemblyDomainType? DeclaredDomain { get; init; }
    
    /// <summary>
    /// Human-readable reason for the failure.
    /// </summary>
    public required string Reason { get; init; }
    
    /// <summary>
    /// When the failure occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Optional exception that caused the failure.
    /// </summary>
    public Exception? Exception { get; init; }
}

/// <summary>
/// Interface for reporting shared assembly resolution failures.
/// </summary>
public interface ISharedAssemblyResolutionReporter
{
    /// <summary>
    /// Reports a resolution failure to the diagnostics channel.
    /// </summary>
    void ReportFailure(SharedAssemblyResolutionFailedEvent failure);
    
    /// <summary>
    /// Gets all reported failures.
    /// </summary>
    IReadOnlyList<SharedAssemblyResolutionFailedEvent> GetReportedFailures();
}

/// <summary>
/// Default implementation that logs failures and stores them for diagnostics.
/// </summary>
public sealed class SharedAssemblyResolutionReporter : ISharedAssemblyResolutionReporter
{
    private readonly Microsoft.Extensions.Logging.ILogger<SharedAssemblyResolutionReporter> _logger;
    private readonly List<SharedAssemblyResolutionFailedEvent> _failures = new();
    private readonly object _lock = new();
    
    public SharedAssemblyResolutionReporter(Microsoft.Extensions.Logging.ILogger<SharedAssemblyResolutionReporter> logger)
    {
        _logger = logger;
    }
    
    public void ReportFailure(SharedAssemblyResolutionFailedEvent failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        
        // Log structured event to management channel
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventType"] = "SharedAssemblyResolutionFailed",
            ["ModuleId"] = failure.ModuleId,
            ["AssemblyName"] = failure.AssemblyName,
            ["Source"] = failure.Source.ToString(),
            ["DeclaredDomain"] = failure.DeclaredDomain?.ToString(),
            ["Timestamp"] = failure.Timestamp
        }))
        {
            if (failure.Exception != null)
            {
                _logger.LogError(
                    failure.Exception,
                    "Shared assembly resolution failed for module {ModuleId}: {AssemblyName} (source: {Source}) - {Reason}",
                    failure.ModuleId, failure.AssemblyName, failure.Source, failure.Reason);
            }
            else
            {
                _logger.LogWarning(
                    "Shared assembly resolution failed for module {ModuleId}: {AssemblyName} (source: {Source}) - {Reason}",
                    failure.ModuleId, failure.AssemblyName, failure.Source, failure.Reason);
            }
        }
        
        lock (_lock)
        {
            _failures.Add(failure);
        }
    }
    
    public IReadOnlyList<SharedAssemblyResolutionFailedEvent> GetReportedFailures()
    {
        lock (_lock)
        {
            return _failures.ToList();
        }
    }
}

