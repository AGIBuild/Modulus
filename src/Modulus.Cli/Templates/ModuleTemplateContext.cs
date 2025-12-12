namespace Modulus.Cli.Templates;

/// <summary>
/// Context data for module template generation.
/// </summary>
public class ModuleTemplateContext
{
    /// <summary>
    /// Module name in PascalCase (e.g., "MyModule").
    /// </summary>
    public required string ModuleName { get; init; }

    /// <summary>
    /// Module name in lowercase (e.g., "mymodule").
    /// </summary>
    public string ModuleNameLower => ModuleName.ToLowerInvariant();

    /// <summary>
    /// Display name shown in menus.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Module description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Publisher name.
    /// </summary>
    public required string Publisher { get; init; }

    /// <summary>
    /// Module unique identifier (GUID).
    /// </summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// Menu icon name (from IconKind).
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Menu order.
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    /// Target host type.
    /// </summary>
    public required TargetHostType TargetHost { get; init; }
}

/// <summary>
/// Target host type for the module.
/// </summary>
public enum TargetHostType
{
    Avalonia,
    Blazor
}

