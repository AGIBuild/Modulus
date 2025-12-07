using System.Collections.Generic;

namespace Modulus.UI.Abstractions;

public interface IMenuRegistry
{
    void Register(MenuItem item);
    void Unregister(string id);
    void UnregisterModuleItems(string moduleId);
    IEnumerable<MenuItem> GetItems(MenuLocation location);
}

