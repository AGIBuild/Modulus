using System.Collections.Concurrent;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor.Shell.Services;

public class MenuRegistry : IMenuRegistry
{
    private readonly ConcurrentBag<UiMenuItem> _items = new();

    public void Register(UiMenuItem item)
    {
        _items.Add(item);
    }

    public IEnumerable<UiMenuItem> GetItems(MenuLocation location)
    {
        return _items.Where(i => i.Location == location).OrderBy(i => i.Order);
    }
}
