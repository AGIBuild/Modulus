using System;

namespace Modulus.Core.Runtime;

/// <summary>
/// Records a module state transition for diagnostics.
/// </summary>
public sealed class ModuleStateTransition
{
    public ModuleState FromState { get; }
    public ModuleState ToState { get; }
    public DateTimeOffset Timestamp { get; }
    public string? Reason { get; }
    public Exception? Exception { get; }

    public ModuleStateTransition(ModuleState fromState, ModuleState toState, string? reason = null, Exception? exception = null)
    {
        FromState = fromState;
        ToState = toState;
        Timestamp = DateTimeOffset.UtcNow;
        Reason = reason;
        Exception = exception;
    }
}


