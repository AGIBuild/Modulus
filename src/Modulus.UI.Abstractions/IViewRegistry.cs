using System;

namespace Modulus.UI.Abstractions;

public interface IViewRegistry
{
    void Register<TViewModel, TView>() where TView : class;
    void Register(Type viewModelType, Type viewType);
    Type? GetViewType(Type viewModelType);
}

