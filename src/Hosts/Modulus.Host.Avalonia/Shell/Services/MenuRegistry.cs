using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Avalonia.Shell.Services;

public class MenuRegistry : IMenuRegistry
{
    private readonly ConcurrentBag<MenuItem> _items = new();

    public void Register(MenuItem item)
    {
        _items.Add(item);
    }

    public IEnumerable<MenuItem> GetItems(MenuLocation location)
    {
        return _items.Where(i => i.Location == location).OrderBy(i => i.Order);
    }
}

