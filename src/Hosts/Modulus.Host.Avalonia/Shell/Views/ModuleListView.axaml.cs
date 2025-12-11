using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Modulus.Host.Avalonia.Shell.ViewModels;

namespace Modulus.Host.Avalonia.Shell.Views;

public partial class ModuleListView : UserControl
{
    public ModuleListView()
    {
        InitializeComponent();
    }

    private async void OnInstallPackageClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Module Package",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Module Package (*.modpkg)")
                {
                    Patterns = new[] { "*.modpkg" }
                }
            }
        });

        if (files.Count == 0) return;

        var file = files.First();
        var path = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return;

        // Call ViewModel to install
        if (DataContext is ModuleListViewModel vm)
        {
            await vm.InstallPackageAsync(path);
        }
    }
}

