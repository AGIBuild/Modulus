namespace Modulus.Sdk;

/// <summary>
/// Declares module metadata. Applied to the Core module class.
/// Version is automatically read from assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ModuleAttribute : Attribute
{
    /// <summary>
    /// Unique module identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Display name shown in UI.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Module description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Module author.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    public ModuleAttribute(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}

