using System;
using System.Collections.Generic;
using System.Linq;

namespace Modulus.Core.Runtime;

/// <summary>
/// Provides diagnostics for module state transitions.
/// </summary>
public sealed class ModuleStateDiagnostics
{
    private readonly List<ModuleStateTransition> _transitions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets all recorded transitions (newest first).
    /// </summary>
    public IReadOnlyList<ModuleStateTransition> Transitions
    {
        get
        {
            lock (_lock)
            {
                return _transitions.AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Records a state transition.
    /// </summary>
    internal void RecordTransition(ModuleState fromState, ModuleState toState, string? reason = null, Exception? exception = null)
    {
        lock (_lock)
        {
            _transitions.Add(new ModuleStateTransition(fromState, toState, reason, exception));
        }
    }

    /// <summary>
    /// Gets the last error from the transition history.
    /// </summary>
    public Exception? GetLastError()
    {
        lock (_lock)
        {
            return _transitions.LastOrDefault(t => t.Exception != null)?.Exception;
        }
    }

    /// <summary>
    /// Gets the last transition reason.
    /// </summary>
    public string? GetLastReason()
    {
        lock (_lock)
        {
            return _transitions.LastOrDefault()?.Reason;
        }
    }

    /// <summary>
    /// Gets time spent in a particular state (if currently in that state).
    /// </summary>
    public TimeSpan? GetTimeInState(ModuleState state)
    {
        lock (_lock)
        {
            var lastTransitionToState = _transitions.LastOrDefault(t => t.ToState == state);
            if (lastTransitionToState == null) return null;

            var nextTransition = _transitions
                .SkipWhile(t => t != lastTransitionToState)
                .Skip(1)
                .FirstOrDefault();

            if (nextTransition != null)
            {
                return nextTransition.Timestamp - lastTransitionToState.Timestamp;
            }

            return DateTimeOffset.UtcNow - lastTransitionToState.Timestamp;
        }
    }

    /// <summary>
    /// Returns a summary of transitions for logging.
    /// </summary>
    public string GetSummary()
    {
        lock (_lock)
        {
            if (_transitions.Count == 0) return "No transitions recorded.";
            
            var states = string.Join(" -> ", _transitions.Select(t => t.ToState.ToString()));
            var lastError = GetLastError();
            var errorInfo = lastError != null ? $" (Last error: {lastError.Message})" : "";
            return $"{states}{errorInfo}";
        }
    }
}


