using System;
using Modulus.Sdk;

namespace Modulus.Core.Runtime;

/// <summary>
/// Represents a module loaded in the runtime, holding its state and load context.
/// </summary>
public sealed class RuntimeModule
{
    private ModuleState _state;
    private readonly object _stateLock = new();

    public event EventHandler<RuntimeModuleStateChangedEventArgs>? StateChanged;

    public ModuleDescriptor Descriptor { get; }
    public ModuleLoadContext LoadContext { get; }
    public string PackagePath { get; }
    public VsixManifest Manifest { get; }
    public bool IsSystem { get; }
    
    /// <summary>
    /// Diagnostics for tracking state transitions.
    /// </summary>
    public ModuleStateDiagnostics Diagnostics { get; } = new();

    /// <summary>
    /// The current state of the module.
    /// </summary>
    public ModuleState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
        set => TransitionTo(value, null, null);
    }

    /// <summary>
    /// The last error that occurred during module lifecycle operations.
    /// </summary>
    public Exception? LastError => Diagnostics.GetLastError();

    public RuntimeModule(ModuleDescriptor descriptor, ModuleLoadContext loadContext, string packagePath, VsixManifest manifest, bool isSystem = false)
    {
        Descriptor = descriptor;
        LoadContext = loadContext;
        PackagePath = packagePath;
        Manifest = manifest;
        _state = ModuleState.Loaded;
        IsSystem = isSystem;
        
        Diagnostics.RecordTransition(ModuleState.Unknown, ModuleState.Loaded, "Module created");
    }

    /// <summary>
    /// Transitions to a new state with an optional reason and error.
    /// </summary>
    public void TransitionTo(ModuleState newState, string? reason, Exception? exception = null)
    {
        ModuleState fromState;
        var changed = false;
        lock (_stateLock)
        {
            if (_state == newState) return;

            fromState = _state;
            _state = newState;
            Diagnostics.RecordTransition(fromState, newState, reason, exception);
            changed = true;
        }

        if (changed)
        {
            StateChanged?.Invoke(this, new RuntimeModuleStateChangedEventArgs(fromState, newState, reason, exception));
        }
    }

    /// <summary>
    /// Validates if a state transition is allowed.
    /// </summary>
    /// <param name="targetState">The desired target state.</param>
    /// <returns>True if transition is valid.</returns>
    public bool CanTransitionTo(ModuleState targetState)
    {
        lock (_stateLock)
        {
            return IsValidTransition(_state, targetState);
        }
    }

    /// <summary>
    /// Checks if a state transition is valid according to the state machine rules.
    /// </summary>
    private static bool IsValidTransition(ModuleState from, ModuleState to)
    {
        // State machine rules:
        // Unknown -> Loaded (initial load)
        // Loaded -> Active (host binding + initialization)
        // Loaded -> Error (initialization failed)
        // Loaded -> Unloaded (unload before activation)
        // Active -> Error (runtime error)
        // Active -> Loaded (deactivation for unload/reload)
        // Active -> Unloaded (full unload)
        // Error -> Loaded (retry/repair)
        // Error -> Unloaded (unload after error)
        // Disabled -> Loaded (re-enable)
        // Disabled -> Unloaded (unload while disabled)
        // Any -> Disabled (disable module)

        if (from == to) return true;

        return (from, to) switch
        {
            (ModuleState.Unknown, ModuleState.Loaded) => true,
            (ModuleState.Loaded, ModuleState.Active) => true,
            (ModuleState.Loaded, ModuleState.Error) => true,
            (ModuleState.Loaded, ModuleState.Unloaded) => true,
            (ModuleState.Loaded, ModuleState.Disabled) => true,
            (ModuleState.Active, ModuleState.Error) => true,
            (ModuleState.Active, ModuleState.Loaded) => true,
            (ModuleState.Active, ModuleState.Unloaded) => true,
            (ModuleState.Active, ModuleState.Disabled) => true,
            (ModuleState.Error, ModuleState.Loaded) => true,
            (ModuleState.Error, ModuleState.Unloaded) => true,
            (ModuleState.Error, ModuleState.Disabled) => true,
            (ModuleState.Disabled, ModuleState.Loaded) => true,
            (ModuleState.Disabled, ModuleState.Unloaded) => true,
            _ => false
        };
    }
}

public sealed class RuntimeModuleStateChangedEventArgs : EventArgs
{
    public RuntimeModuleStateChangedEventArgs(ModuleState fromState, ModuleState toState, string? reason, Exception? exception)
    {
        FromState = fromState;
        ToState = toState;
        Reason = reason;
        Exception = exception;
    }

    public ModuleState FromState { get; }
    public ModuleState ToState { get; }
    public string? Reason { get; }
    public Exception? Exception { get; }
}
