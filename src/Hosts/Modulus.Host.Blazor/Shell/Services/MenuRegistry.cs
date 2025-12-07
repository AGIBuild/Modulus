using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor.Shell.Services;

public class MenuRegistry : IMenuRegistry
{
    private readonly ConcurrentDictionary<string, UiMenuItem> _items = new(StringComparer.OrdinalIgnoreCase);

    public void Register(UiMenuItem item)
    {
        _items[item.Id] = item;
    }

    public void Unregister(string id)
    {
        _items.TryRemove(id, out _);
    }

    public void UnregisterModuleItems(string moduleId)
    {
        var itemsToRemove = _items.Values.Where(i => i.ModuleId == moduleId).Select(i => i.Id).ToList();
        foreach (var id in itemsToRemove)
        {
            _items.TryRemove(id, out _);
        }
    }

    public IEnumerable<UiMenuItem> GetItems(MenuLocation location)
    {
        return _items.Values.Where(i => i.Location == location).OrderBy(i => i.Order);
    }
}
