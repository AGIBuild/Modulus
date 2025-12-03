using System;

namespace Modulus.Sdk;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    public Type[] DependedModuleTypes { get; }

    public DependsOnAttribute(params Type[] dependedModuleTypes)
    {
        DependedModuleTypes = dependedModuleTypes ?? Type.EmptyTypes;
    }
}

