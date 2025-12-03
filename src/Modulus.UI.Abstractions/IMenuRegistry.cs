using System;
using System.Collections.Generic;

namespace Modulus.UI.Abstractions;

public interface IMenuRegistry
{
    void Register(MenuItem item);
    IEnumerable<MenuItem> GetItems(MenuLocation location);
}

