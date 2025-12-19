using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data.Models;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Runtime;

/// <summary>
/// Builds hierarchical <see cref="MenuItem"/> trees from database menu rows.
/// </summary>
public static class MenuTreeBuilder
{
    public static IReadOnlyList<MenuItem> Build(IReadOnlyList<MenuEntity> menus, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(menus);
        ArgumentNullException.ThrowIfNull(logger);

        var childrenByParentId = menus
            .Where(m => !string.IsNullOrWhiteSpace(m.ParentId))
            .GroupBy(m => m.ParentId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(m => m.Order)
                    .ThenBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var roots = menus
            .Where(m => string.IsNullOrWhiteSpace(m.ParentId))
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        MenuItem BuildItem(MenuEntity menu, HashSet<string> path)
        {
            if (!path.Add(menu.Id))
            {
                // Cycle guard: ignore children if we detect a loop.
                return CreateLeaf(menu);
            }

            if (childrenByParentId.TryGetValue(menu.Id, out var children) && children.Count > 0)
            {
                var childItems = children.Select(c => BuildItem(c, path)).ToList();
                path.Remove(menu.Id);

                var group = MenuItem.CreateGroup(
                    menu.Id,
                    menu.DisplayName,
                    ParseIcon(menu.Icon),
                    childItems,
                    NormalizeLocation(menu, logger),
                    menu.Order);

                group.ModuleId = menu.ModuleId;
                return group;
            }

            path.Remove(menu.Id);
            return CreateLeaf(menu);
        }

        MenuItem CreateLeaf(MenuEntity menu)
        {
            var item = new MenuItem(
                menu.Id,
                menu.DisplayName,
                ParseIcon(menu.Icon),
                menu.Route ?? menu.Id,
                NormalizeLocation(menu, logger),
                menu.Order);

            item.ModuleId = menu.ModuleId;
            return item;
        }

        var builtRoots = new List<MenuItem>(roots.Count);
        foreach (var root in roots)
        {
            builtRoots.Add(BuildItem(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
        }

        return builtRoots;
    }

    private static IconKind ParseIcon(string? icon)
    {
        if (!string.IsNullOrWhiteSpace(icon) && Enum.TryParse<IconKind>(icon, true, out var parsed))
            return parsed;
        return IconKind.Grid;
    }

    private static MenuLocation NormalizeLocation(MenuEntity menu, ILogger logger)
    {
        var isSystemModule = menu.Module?.IsSystem ?? false;
        if (isSystemModule) return menu.Location;

        if (menu.Location == MenuLocation.Bottom)
        {
            logger.LogWarning("Module {ModuleId} is not system-managed but has Bottom menu location. Forcing to Main.", menu.ModuleId);
        }

        return MenuLocation.Main;
    }
}


