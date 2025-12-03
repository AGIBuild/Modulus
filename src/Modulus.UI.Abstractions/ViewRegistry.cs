using System;
using System.Collections.Concurrent;

namespace Modulus.UI.Abstractions;

public class ViewRegistry : IViewRegistry
{
    private readonly ConcurrentDictionary<Type, Type> _mappings = new();

    public void Register<TViewModel, TView>() where TView : class
    {
        Register(typeof(TViewModel), typeof(TView));
    }

    public void Register(Type viewModelType, Type viewType)
    {
        _mappings[viewModelType] = viewType;
    }

    public Type? GetViewType(Type viewModelType)
    {
        return _mappings.TryGetValue(viewModelType, out var viewType) ? viewType : null;
    }
}

