using System;
using System.Collections.Generic;

namespace Modulus.UI.Abstractions;

public interface IMenuRegistry
{
    event EventHandler MenuChanged;
    void Register(MenuItem item);
    void Unregister(string id);
    IEnumerable<MenuItem> GetItems(MenuLocation location);
}

