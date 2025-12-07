using System;
using Modulus.Sdk;

namespace Modulus.Core.Runtime;

/// <summary>
/// Represents a module loaded in the runtime, holding its state and load context.
/// </summary>
public sealed class RuntimeModule
{
    public ModuleDescriptor Descriptor { get; }
    public ModuleLoadContext LoadContext { get; }
    public string PackagePath { get; }
    public ModuleManifest Manifest { get; }
    
    public ModuleState State { get; set; }
    public Exception? LastError { get; set; }
    public bool IsSystem { get; }

    public RuntimeModule(ModuleDescriptor descriptor, ModuleLoadContext loadContext, string packagePath, ModuleManifest manifest, bool isSystem = false)
    {
        Descriptor = descriptor;
        LoadContext = loadContext;
        PackagePath = packagePath;
        Manifest = manifest;
        State = ModuleState.Loaded;
        IsSystem = isSystem;
    }
}

