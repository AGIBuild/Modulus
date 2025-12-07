using System.Collections.Generic;

namespace Modulus.UI.Abstractions.Messages;

/// <summary>
/// Message sent when menus need to be refreshed (e.g., after module enable/disable).
/// </summary>
public record MenuRefreshMessage;

/// <summary>
/// Message sent when menu items should be added to the navigation.
/// </summary>
public record MenuItemsAddedMessage(IReadOnlyList<MenuItem> Items);

/// <summary>
/// Message sent when menu items should be removed from the navigation.
/// </summary>
public record MenuItemsRemovedMessage(string ModuleId);

