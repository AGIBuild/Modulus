namespace Modulus.Core.Runtime;

public enum ModuleState
{
    Unknown,
    Loaded,
    Active,   // Initialized and running
    Disabled, // Loaded but explicitly disabled
    Error,    // Failed to load or initialize
    Unloaded  // Removed from memory (transient state before GC)
}

