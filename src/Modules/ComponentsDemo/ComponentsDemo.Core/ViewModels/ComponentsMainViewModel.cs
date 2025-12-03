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
        // Navigation group - contains all navigation-related demos
        var contextDemo = new MenuItem("context-demo", "Context Menu", "list", "context-demo", MenuLocation.Main, 5);
        contextDemo.ContextActions = new[]
        {
            new MenuAction { Label = "Edit", Icon = "edit", Execute = _ => { } },
            new MenuAction { Label = "Delete", Icon = "delete", Execute = _ => { } },
            new MenuAction { Label = "Info", Icon = "info", Execute = _ => { } }
        };

        var lifecycleDemo = new MenuItem("lifecycle-demo", "Page Lifecycle", "refresh", "lifecycle-demo", MenuLocation.Main, 6);
        lifecycleDemo.InstanceMode = PageInstanceMode.Transient;

        var navigationGroup = MenuItem.CreateGroup("navigation", "Navigation", "compass", new[]
        {
            new MenuItem("basic-nav", "Basic Navigation", "link", "basic-nav", MenuLocation.Main, 0),
            new MenuItem("badge-nav", "Badge Indicators", "notifications", "badge-nav", MenuLocation.Main, 1) 
            { 
                BadgeCount = 5, 
                BadgeColor = "error" 
            },
            new MenuItem("disabled-nav", "Disabled States", "block", "disabled-nav", MenuLocation.Main, 2),
            MenuItem.CreateGroup("sub-menu", "Sub Menu Demo", "folder", new[]
            {
                new MenuItem("sub-item-1", "Sub Item 1", "file", "sub-item-1", MenuLocation.Main, 0),
                new MenuItem("sub-item-2", "Sub Item 2", "file", "sub-item-2", MenuLocation.Main, 1),
                new MenuItem("sub-item-3", "Sub Item 3 (Disabled)", "file", "sub-item-3", MenuLocation.Main, 2) { IsEnabled = false }
            }, MenuLocation.Main, 3),
            new MenuItem("keyboard-nav", "Keyboard Navigation", "keyboard", "keyboard-nav", MenuLocation.Main, 4),
            contextDemo,
            lifecycleDemo
        }, MenuLocation.Main, 0);
        navigationGroup.IsExpanded = true;

        DemoItems.Add(navigationGroup);
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
            "basic-nav" => "Basic Navigation",
            "badge-nav" => "Badge Indicators",
            "disabled-nav" => "Disabled States",
            "sub-item-1" => "Sub Item 1",
            "sub-item-2" => "Sub Item 2",
            "sub-item-3" => "Sub Item 3",
            "keyboard-nav" => "Keyboard Navigation",
            "context-demo" => "Context Menu",
            "lifecycle-demo" => "Page Lifecycle",
            _ => "Basic Navigation"
        };
    }
}

