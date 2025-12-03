using System;
using System.Linq;
using Modulus.Core.Runtime;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Blazor.Services;

public class BlazorUIFactory : IUIFactory
{
    private readonly RuntimeContext _runtimeContext;

    public BlazorUIFactory(RuntimeContext runtimeContext)
    {
        _runtimeContext = runtimeContext;
    }

    public object CreateView(object viewModel)
    {
        var vmType = viewModel.GetType();
        var viewName = vmType.Name.Replace("ViewModel", "View"); // e.g. NoteListView
        // Or ModuleListView (from ModuleListViewModel)

        foreach (var module in _runtimeContext.RuntimeModules)
        {
            if (module.State != ModuleState.Active && module.State != ModuleState.Loaded) continue;

            foreach (var asm in module.LoadContext.Assemblies)
            {
                var type = asm.GetTypes().FirstOrDefault(t => t.Name == viewName);
                if (type != null)
                {
                    return type;
                }
            }
        }
        
        return null!;
    }

    public object CreateView(string viewKey)
    {
        throw new NotImplementedException();
    }
}

