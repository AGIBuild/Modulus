using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ComponentsDemo.ViewModels;

/// <summary>
/// Main ViewModel for the Components Demo page with internal navigation.
/// </summary>
public partial class ComponentsMainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentDemoName = "Navigation Demo";

    [ObservableProperty]
    private string _currentDemoId = "nav-demo";

    public ObservableCollection<MenuItem> DemoItems { get; } = new();

    public ICommand NavigateCommand { get; }
    public ICommand ToggleGroupCommand { get; }

    public ComponentsMainViewModel()
    {
        NavigateCommand = new RelayCommand<MenuItem>(OnNavigate);
        ToggleGroupCommand = new RelayCommand<MenuItem>(OnToggleGroup);
        InitializeDemoItems();
    }

    private void OnNavigate(MenuItem? item)
    {
        if (item == null || !item.IsEnabled) return;
        if (!string.IsNullOrEmpty(item.NavigationKey))
        {
            NavigateTo(item.NavigationKey);
        }
    }

    private void OnToggleGroup(MenuItem? item)
    {
        if (item != null)
        {
            item.IsExpanded = !item.IsExpanded;
        }
    }

    private void InitializeDemoItems()
    {
        // Navigation Demo - showcases all navigation features
        var navDemo = new MenuItem("nav-demo", "Navigation Demo", "🧭", "nav-demo", MenuLocation.Main, 0);
        navDemo.BadgeCount = 3;
        navDemo.BadgeColor = "info";

        // Badge Demo
        var badgeDemo = new MenuItem("badge-demo", "Badge Demo", "🔢", "badge-demo", MenuLocation.Main, 1);
        badgeDemo.BadgeCount = 99;
        badgeDemo.BadgeColor = "error";

        // Disabled State Demo
        var disabledDemo = new MenuItem("disabled-demo", "Disabled Demo", "🚫", "disabled-demo", MenuLocation.Main, 2);

        // Hierarchical Menu Demo
        var hierarchyDemo = MenuItem.CreateGroup("hierarchy-demo", "Hierarchical Demo", "📁", new[]
        {
            new MenuItem("sub-item-1", "Sub Item 1", "📄", "sub-item-1", MenuLocation.Main, 0),
            new MenuItem("sub-item-2", "Sub Item 2", "📄", "sub-item-2", MenuLocation.Main, 1),
            new MenuItem("sub-item-3", "Sub Item 3 (Disabled)", "📄", "sub-item-3", MenuLocation.Main, 2) { IsEnabled = false }
        }, MenuLocation.Main, 3);

        // Context Menu Demo
        var contextDemo = new MenuItem("context-demo", "Context Menu Demo", "📋", "context-demo", MenuLocation.Main, 4);
        contextDemo.ContextActions = new[]
        {
            new MenuAction { Label = "Action 1", Icon = "✏️", Execute = _ => { } },
            new MenuAction { Label = "Action 2", Icon = "🗑️", Execute = _ => { } },
            new MenuAction { Label = "Action 3", Icon = "ℹ️", Execute = _ => { } }
        };

        // Keyboard Nav Demo
        var keyboardDemo = new MenuItem("keyboard-demo", "Keyboard Navigation", "⌨️", "keyboard-demo", MenuLocation.Main, 5);

        // Page Lifecycle Demo
        var lifecycleDemo = new MenuItem("lifecycle-demo", "Page Lifecycle", "🔄", "lifecycle-demo", MenuLocation.Main, 6);
        lifecycleDemo.InstanceMode = PageInstanceMode.Transient;

        DemoItems.Add(navDemo);
        DemoItems.Add(badgeDemo);
        DemoItems.Add(disabledDemo);
        DemoItems.Add(hierarchyDemo);
        DemoItems.Add(contextDemo);
        DemoItems.Add(keyboardDemo);
        DemoItems.Add(lifecycleDemo);
    }

    public void NavigateTo(string demoId)
    {
        CurrentDemoId = demoId;
        CurrentDemoName = GetDemoName(demoId);
    }

    private string GetDemoName(string demoId)
    {
        return demoId switch
        {
            "nav-demo" => "Navigation Demo",
            "badge-demo" => "Badge Demo",
            "disabled-demo" => "Disabled Demo",
            "sub-item-1" => "Sub Item 1",
            "sub-item-2" => "Sub Item 2",
            "sub-item-3" => "Sub Item 3",
            "context-demo" => "Context Menu Demo",
            "keyboard-demo" => "Keyboard Navigation",
            "lifecycle-demo" => "Page Lifecycle",
            _ => "Navigation Demo"
        };
    }
}

