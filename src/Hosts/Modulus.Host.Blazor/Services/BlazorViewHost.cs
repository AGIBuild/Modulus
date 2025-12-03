using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Blazor.Services;

public class BlazorViewHost : IViewHost
{
    // For MVP, we might just expose the current view type to the main layout via an event or service.
    // In MAUI Blazor, we are still running inside a WebView, so the logic is similar to Blazor Server.
    
    public object? CurrentViewType { get; private set; }
    public event Action? ViewChanged;

    public Task ShowViewAsync(object view, string title, CancellationToken cancellationToken = default)
    {
        if (view is Type viewType)
        {
            CurrentViewType = viewType;
            ViewChanged?.Invoke();
        }
        return Task.CompletedTask;
    }

    public Task<bool?> ShowDialogAsync(object view, string title, CancellationToken cancellationToken = default)
    {
        // Integration with MudBlazor DialogService would go here
        return Task.FromResult<bool?>(false);
    }

    public Task CloseViewAsync(object view, CancellationToken cancellationToken = default)
    {
        CurrentViewType = null;
        ViewChanged?.Invoke();
        return Task.CompletedTask;
    }
}

