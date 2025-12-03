using System;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Represents a context menu action for a MenuItem.
/// </summary>
public class MenuAction
{
    /// <summary>
    /// Display label for the action.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Optional icon identifier for the action.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Callback executed when the action is triggered.
    /// </summary>
    public required Action<MenuItem> Execute { get; init; }
}

