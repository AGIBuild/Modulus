namespace Modulus.Core.Runtime;

/// <summary>
/// Represents the health state of a module.
/// </summary>
public enum ModuleHealthState
{
    /// <summary>Module is running normally with no recent faults.</summary>
    Healthy,
    
    /// <summary>Module has experienced faults but is still operational.</summary>
    Degraded,
    
    /// <summary>Module has critical faults and has been disabled.</summary>
    Faulted,
    
    /// <summary>Module has been unloaded.</summary>
    Unloaded
}

/// <summary>
/// Records a single fault event for a module.
/// </summary>
public class ModuleFaultRecord
{
    public DateTime Timestamp { get; init; }
    public string? Caller { get; init; }
    public string ExceptionType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    
    public static ModuleFaultRecord FromException(Exception ex, string? caller = null) => new()
    {
        Timestamp = DateTime.UtcNow,
        Caller = caller,
        ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
        Message = ex.Message,
        StackTrace = ex.StackTrace
    };
}

/// <summary>
/// Tracks the health status and fault history of a module.
/// </summary>
public class ModuleHealthInfo
{
    private readonly object _lock = new();
    private readonly List<ModuleFaultRecord> _faultHistory = new();
    
    /// <summary>Current health state of the module.</summary>
    public ModuleHealthState State { get; private set; } = ModuleHealthState.Healthy;
    
    /// <summary>Total number of faults since module was loaded.</summary>
    public int TotalFaultCount { get; private set; }
    
    /// <summary>Number of consecutive faults without successful execution.</summary>
    public int ConsecutiveFaultCount { get; private set; }
    
    /// <summary>Most recent exception that occurred.</summary>
    public Exception? LastError { get; private set; }
    
    /// <summary>Timestamp of the most recent fault.</summary>
    public DateTime? LastFaultTime { get; private set; }
    
    /// <summary>Timestamp when module was last in Healthy state.</summary>
    public DateTime LastHealthyTime { get; private set; } = DateTime.UtcNow;
    
    /// <summary>Recent fault records (capped at MaxHistorySize).</summary>
    public IReadOnlyList<ModuleFaultRecord> FaultHistory
    {
        get
        {
            lock (_lock)
            {
                return _faultHistory.ToList();
            }
        }
    }
    
    private const int MaxHistorySize = 20;
    
    /// <summary>
    /// Records a fault and updates health state.
    /// </summary>
    public void RecordFault(Exception ex, string? caller = null)
    {
        lock (_lock)
        {
            TotalFaultCount++;
            ConsecutiveFaultCount++;
            LastError = ex;
            LastFaultTime = DateTime.UtcNow;
            
            _faultHistory.Add(ModuleFaultRecord.FromException(ex, caller));
            
            // Keep history bounded
            while (_faultHistory.Count > MaxHistorySize)
            {
                _faultHistory.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// Records a successful execution, resetting consecutive fault count.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            ConsecutiveFaultCount = 0;
        }
    }
    
    /// <summary>
    /// Transitions to a new health state.
    /// </summary>
    public void TransitionTo(ModuleHealthState newState)
    {
        lock (_lock)
        {
            if (State == newState) return;
            
            var oldState = State;
            State = newState;
            
            if (newState == ModuleHealthState.Healthy)
            {
                LastHealthyTime = DateTime.UtcNow;
            }
        }
    }
    
    /// <summary>
    /// Resets health info to initial state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            State = ModuleHealthState.Healthy;
            TotalFaultCount = 0;
            ConsecutiveFaultCount = 0;
            LastError = null;
            LastFaultTime = null;
            LastHealthyTime = DateTime.UtcNow;
            _faultHistory.Clear();
        }
    }
}

