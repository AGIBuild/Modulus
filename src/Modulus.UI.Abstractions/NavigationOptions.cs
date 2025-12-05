using System.Collections.Generic;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Options for navigation requests.
/// </summary>
public class NavigationOptions
{
    /// <summary>
    /// When true, creates a new instance regardless of the target's InstanceMode setting.
    /// </summary>
    public bool ForceNewInstance { get; init; }

    /// <summary>
    /// Optional parameters to pass to the target view/viewmodel.
    /// </summary>
    public IDictionary<string, object>? Parameters { get; init; }
}

