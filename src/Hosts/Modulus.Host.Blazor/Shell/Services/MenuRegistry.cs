using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor.Shell.Services;

public class MenuRegistry : IMenuRegistry
{
    private readonly ConcurrentDictionary<string, UiMenuItem> _items = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? MenuChanged;

    public void Register(UiMenuItem item)
    {
        _items[item.Id] = item;
        MenuChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Unregister(string id)
    {
        if (_items.TryRemove(id, out _))
        {
            MenuChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public IEnumerable<UiMenuItem> GetItems(MenuLocation location)
    {
        return _items.Values.Where(i => i.Location == location).OrderBy(i => i.Order);
    }
}
