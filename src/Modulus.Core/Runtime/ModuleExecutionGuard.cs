using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Modulus.Core.Runtime;

/// <summary>
/// Provides exception isolation for module execution.
/// Prevents module faults from crashing the host application.
/// </summary>
public interface IModuleExecutionGuard
{
    /// <summary>
    /// Safely executes a synchronous action for a module.
    /// </summary>
    TResult? ExecuteSafe<TResult>(
        string moduleId,
        Func<TResult> action,
        TResult? fallback = default,
        [CallerMemberName] string? caller = null);

    /// <summary>
    /// Safely executes an asynchronous action for a module.
    /// </summary>
    Task<TResult?> ExecuteSafeAsync<TResult>(
        string moduleId,
        Func<Task<TResult>> action,
        TResult? fallback = default,
        [CallerMemberName] string? caller = null);

    /// <summary>
    /// Safely executes an asynchronous action without return value.
    /// </summary>
    Task ExecuteSafeAsync(
        string moduleId,
        Func<Task> action,
        [CallerMemberName] string? caller = null);

    /// <summary>
    /// Gets the health info for a module.
    /// </summary>
    ModuleHealthInfo GetHealthInfo(string moduleId);

    /// <summary>
    /// Checks if a module is in a state that allows execution.
    /// </summary>
    bool CanExecute(string moduleId);

    /// <summary>
    /// Resets the health state of a module (for recovery attempts).
    /// </summary>
    void ResetHealth(string moduleId);

    /// <summary>
    /// Marks a module as faulted and triggers unload if needed.
    /// </summary>
    Task MarkAsFaultedAsync(string moduleId, string reason);

    /// <summary>
    /// Event raised when a module's health state changes.
    /// </summary>
    event EventHandler<ModuleHealthChangedEventArgs>? HealthChanged;
}

public class ModuleHealthChangedEventArgs : EventArgs
{
    public string ModuleId { get; }
    public ModuleHealthState OldState { get; }
    public ModuleHealthState NewState { get; }
    public Exception? Cause { get; }

    public ModuleHealthChangedEventArgs(string moduleId, ModuleHealthState oldState, ModuleHealthState newState, Exception? cause = null)
    {
        ModuleId = moduleId;
        OldState = oldState;
        NewState = newState;
        Cause = cause;
    }
}

public class ModuleExecutionGuard : IModuleExecutionGuard
{
    private readonly ConcurrentDictionary<string, ModuleHealthInfo> _healthMap = new();
    private readonly ILogger<ModuleExecutionGuard> _logger;
    private readonly RuntimeContext _runtimeContext;

    // Configuration thresholds
    private const int MaxConsecutiveFaults = 3;
    private const int MaxTotalFaults = 10;
    private static readonly TimeSpan FaultRecoveryWindow = TimeSpan.FromMinutes(5);

    public event EventHandler<ModuleHealthChangedEventArgs>? HealthChanged;

    public ModuleExecutionGuard(
        ILogger<ModuleExecutionGuard> logger,
        RuntimeContext runtimeContext)
    {
        _logger = logger;
        _runtimeContext = runtimeContext;
    }

    public TResult? ExecuteSafe<TResult>(
        string moduleId,
        Func<TResult> action,
        TResult? fallback = default,
        [CallerMemberName] string? caller = null)
    {
        var health = GetOrCreateHealth(moduleId);

        if (!CanExecuteInternal(health, moduleId, caller))
        {
            return fallback;
        }

        try
        {
            var result = action();
            OnExecutionSuccess(moduleId, health);
            return result;
        }
        catch (Exception ex)
        {
            HandleFault(moduleId, health, ex, caller);
            return fallback;
        }
    }

    public async Task<TResult?> ExecuteSafeAsync<TResult>(
        string moduleId,
        Func<Task<TResult>> action,
        TResult? fallback = default,
        [CallerMemberName] string? caller = null)
    {
        var health = GetOrCreateHealth(moduleId);

        if (!CanExecuteInternal(health, moduleId, caller))
        {
            return fallback;
        }

        try
        {
            var result = await action().ConfigureAwait(false);
            OnExecutionSuccess(moduleId, health);
            return result;
        }
        catch (Exception ex)
        {
            await HandleFaultAsync(moduleId, health, ex, caller).ConfigureAwait(false);
            return fallback;
        }
    }

    public async Task ExecuteSafeAsync(
        string moduleId,
        Func<Task> action,
        [CallerMemberName] string? caller = null)
    {
        var health = GetOrCreateHealth(moduleId);

        if (!CanExecuteInternal(health, moduleId, caller))
        {
            return;
        }

        try
        {
            await action().ConfigureAwait(false);
            OnExecutionSuccess(moduleId, health);
        }
        catch (Exception ex)
        {
            await HandleFaultAsync(moduleId, health, ex, caller).ConfigureAwait(false);
        }
    }

    public ModuleHealthInfo GetHealthInfo(string moduleId)
    {
        return GetOrCreateHealth(moduleId);
    }

    public bool CanExecute(string moduleId)
    {
        var health = GetOrCreateHealth(moduleId);
        return health.State != ModuleHealthState.Faulted && 
               health.State != ModuleHealthState.Unloaded;
    }

    public void ResetHealth(string moduleId)
    {
        if (_healthMap.TryGetValue(moduleId, out var health))
        {
            var oldState = health.State;
            health.Reset();
            
            _logger.LogInformation("Module {ModuleId} health reset from {OldState} to Healthy", 
                moduleId, oldState);
            
            RaiseHealthChanged(moduleId, oldState, ModuleHealthState.Healthy);
        }
    }

    public async Task MarkAsFaultedAsync(string moduleId, string reason)
    {
        var health = GetOrCreateHealth(moduleId);
        var oldState = health.State;
        
        health.TransitionTo(ModuleHealthState.Faulted);
        
        _logger.LogWarning("Module {ModuleId} manually marked as FAULTED: {Reason}", 
            moduleId, reason);
        
        RaiseHealthChanged(moduleId, oldState, ModuleHealthState.Faulted);
        
        // Attempt to unload the faulted module
        await TryUnloadModuleAsync(moduleId).ConfigureAwait(false);
    }

    private ModuleHealthInfo GetOrCreateHealth(string moduleId)
    {
        return _healthMap.GetOrAdd(moduleId, _ => new ModuleHealthInfo());
    }

    private bool CanExecuteInternal(ModuleHealthInfo health, string moduleId, string? caller)
    {
        if (health.State == ModuleHealthState.Faulted)
        {
            _logger.LogDebug(
                "Skipping execution for faulted module {ModuleId}. Caller: {Caller}",
                moduleId, caller);
            return false;
        }

        if (health.State == ModuleHealthState.Unloaded)
        {
            _logger.LogDebug(
                "Skipping execution for unloaded module {ModuleId}. Caller: {Caller}",
                moduleId, caller);
            return false;
        }

        return true;
    }

    private void OnExecutionSuccess(string moduleId, ModuleHealthInfo health)
    {
        if (health.ConsecutiveFaultCount > 0)
        {
            health.RecordSuccess();

            // Check if module should recover from Degraded to Healthy
            if (health.State == ModuleHealthState.Degraded &&
                health.LastFaultTime.HasValue &&
                DateTime.UtcNow - health.LastFaultTime.Value > FaultRecoveryWindow)
            {
                var oldState = health.State;
                health.TransitionTo(ModuleHealthState.Healthy);
                
                _logger.LogInformation(
                    "Module {ModuleId} recovered to Healthy state after {Window} without faults",
                    moduleId, FaultRecoveryWindow);
                
                RaiseHealthChanged(moduleId, oldState, ModuleHealthState.Healthy);
            }
        }
    }

    private void HandleFault(string moduleId, ModuleHealthInfo health, Exception ex, string? caller)
    {
        RecordFaultAndLog(moduleId, health, ex, caller);
        EvaluateHealthTransition(moduleId, health, ex);
    }

    private async Task HandleFaultAsync(string moduleId, ModuleHealthInfo health, Exception ex, string? caller)
    {
        RecordFaultAndLog(moduleId, health, ex, caller);
        
        var needsUnload = EvaluateHealthTransition(moduleId, health, ex);
        
        if (needsUnload)
        {
            await TryUnloadModuleAsync(moduleId).ConfigureAwait(false);
        }
    }

    private void RecordFaultAndLog(string moduleId, ModuleHealthInfo health, Exception ex, string? caller)
    {
        health.RecordFault(ex, caller);

        _logger.LogError(ex,
            "Module {ModuleId} fault in {Caller}. Consecutive: {Consecutive}, Total: {Total}",
            moduleId, caller, health.ConsecutiveFaultCount, health.TotalFaultCount);
    }

    private bool EvaluateHealthTransition(string moduleId, ModuleHealthInfo health, Exception ex)
    {
        var oldState = health.State;
        var needsUnload = false;

        // Check if module should be marked as Faulted
        if (health.ConsecutiveFaultCount >= MaxConsecutiveFaults ||
            health.TotalFaultCount >= MaxTotalFaults)
        {
            health.TransitionTo(ModuleHealthState.Faulted);
            
            _logger.LogWarning(
                "Module {ModuleId} marked as FAULTED. Consecutive faults: {Consecutive}, Total: {Total}",
                moduleId, health.ConsecutiveFaultCount, health.TotalFaultCount);
            
            RaiseHealthChanged(moduleId, oldState, ModuleHealthState.Faulted, ex);
            needsUnload = true;
        }
        else if (health.State == ModuleHealthState.Healthy)
        {
            // Degrade from Healthy to Degraded on first fault
            health.TransitionTo(ModuleHealthState.Degraded);
            
            _logger.LogWarning(ex, 
                "Module {ModuleId} degraded due to fault. Exception: {ExceptionType}: {Message}", 
                moduleId, ex.GetType().Name, ex.Message);
            RaiseHealthChanged(moduleId, oldState, ModuleHealthState.Degraded, ex);
        }

        return needsUnload;
    }

    private async Task TryUnloadModuleAsync(string moduleId)
    {
        try
        {
            if (_runtimeContext.TryGetModule(moduleId, out var runtimeModule))
            {
                _logger.LogInformation("Attempting to unload faulted module {ModuleId}...", moduleId);
                
                // Get ModuleLoader from runtime module's service provider if available
                // For now, we just mark the module as unloaded in our health tracking
                // The actual unload should be triggered by the caller
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload faulted module {ModuleId}", moduleId);
        }
    }

    private void RaiseHealthChanged(string moduleId, ModuleHealthState oldState, ModuleHealthState newState, Exception? cause = null)
    {
        try
        {
            HealthChanged?.Invoke(this, new ModuleHealthChangedEventArgs(moduleId, oldState, newState, cause));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HealthChanged event handler");
        }
    }

    /// <summary>
    /// Called when a module is unloaded to update health tracking.
    /// </summary>
    public void OnModuleUnloaded(string moduleId)
    {
        if (_healthMap.TryGetValue(moduleId, out var health))
        {
            var oldState = health.State;
            health.TransitionTo(ModuleHealthState.Unloaded);
            RaiseHealthChanged(moduleId, oldState, ModuleHealthState.Unloaded);
        }
    }

    /// <summary>
    /// Removes health tracking for a module (when module is fully removed).
    /// </summary>
    public void RemoveHealthTracking(string moduleId)
    {
        _healthMap.TryRemove(moduleId, out _);
    }
}

