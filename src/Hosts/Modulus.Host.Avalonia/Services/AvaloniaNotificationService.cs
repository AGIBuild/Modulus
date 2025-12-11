using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Controls;

namespace Modulus.Host.Avalonia.Services;

/// <summary>
/// Avalonia implementation of INotificationService using themed MessageDialog.
/// </summary>
public class AvaloniaNotificationService : INotificationService
{
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        await MessageDialog.ShowInfoAsync(mainWindow, title, message);
    }

    public async Task ShowWarningAsync(string title, string message)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        await MessageDialog.ShowWarningAsync(mainWindow, title, message);
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        await MessageDialog.ShowErrorAsync(mainWindow, title, message);
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmLabel = "OK", string cancelLabel = "Cancel")
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return false;

        return await MessageDialog.ConfirmAsync(mainWindow, title, message, confirmLabel, cancelLabel);
    }
}

