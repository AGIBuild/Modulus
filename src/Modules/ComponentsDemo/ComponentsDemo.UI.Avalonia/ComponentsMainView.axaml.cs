using Avalonia.Controls;
using Modulus.Modules.ComponentsDemo.ViewModels;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia;

public partial class ComponentsMainView : UserControl
{
    public ComponentsMainView()
    {
        InitializeComponent();
    }

    private void OnNavigationItemSelected(object? sender, UiMenuItem item)
    {
        if (DataContext is ComponentsMainViewModel vm && !string.IsNullOrEmpty(item.NavigationKey))
        {
            vm.NavigateTo(item.NavigationKey);
        }
    }
}

