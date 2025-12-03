using Avalonia;
using Avalonia.Controls;
using Modulus.Modules.ComponentsDemo.ViewModels;
using Modulus.UI.Avalonia.Controls;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia;

public partial class ComponentsMainView : UserControl
{
    private NavigationView? _navView;

    public ComponentsMainView()
    {
        InitializeComponent();
        
        // Find the NavigationView after initialization
        _navView = this.FindControl<NavigationView>("DemoNavView");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        // Manually bind items when DataContext changes
        if (DataContext is ComponentsMainViewModel vm && _navView != null)
        {
            System.Diagnostics.Debug.WriteLine($"[ComponentsMainView] DataContext set, DemoItems count: {vm.DemoItems.Count}");
            _navView.Items = vm.DemoItems;
        }
    }

    private void OnNavigationItemSelected(object? sender, UiMenuItem item)
    {
        if (DataContext is ComponentsMainViewModel vm && !string.IsNullOrEmpty(item.NavigationKey))
        {
            vm.NavigateTo(item.NavigationKey);
        }
    }
}

