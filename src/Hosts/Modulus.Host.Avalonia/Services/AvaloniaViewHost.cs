using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Avalonia.Services;

public class AvaloniaViewHost : IViewHost
{
    private readonly Window _mainWindow;
    
    // In a real app, this would be more sophisticated (Region Manager).
    // For MVP, we set Content of the Window.

    public AvaloniaViewHost(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public Task ShowViewAsync(object view, string title, CancellationToken cancellationToken = default)
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (view is Control control)
            {
                _mainWindow.Content = control;
                if (!string.IsNullOrEmpty(title))
                {
                    _mainWindow.Title = title;
                }
            }
        }).GetTask();
    }

    public Task<bool?> ShowDialogAsync(object view, string title, CancellationToken cancellationToken = default)
    {
        // MVP: Just show as content or simple dialog
        // Implementation omitted for brevity
        return Task.FromResult<bool?>(false);
    }

    public Task CloseViewAsync(object view, CancellationToken cancellationToken = default)
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
        {
            _mainWindow.Content = null;
        }).GetTask();
    }
}

