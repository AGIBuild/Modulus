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
    private string _currentDemoName = "Basic Navigation";

    [ObservableProperty]
    private string _currentDemoId = "basic-nav";

    [ObservableProperty]
    private MenuItem? _selectedDemoItem;

    public ObservableCollection<MenuItem> DemoItems { get; } = new();

    public ICommand NavigateCommand { get; }
    public ICommand ToggleGroupCommand { get; }

    public ComponentsMainViewModel()
    {
        NavigateCommand = new RelayCommand<MenuItem>(OnNavigate);
        ToggleGroupCommand = new RelayCommand<MenuItem>(OnToggleGroup);
        InitializeDemoItems();
    }

    partial void OnSelectedDemoItemChanged(MenuItem? value)
    {
        if (value != null && !string.IsNullOrEmpty(value.NavigationKey))
        {
            NavigateTo(value.NavigationKey);
        }
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
        // Uses IconKind enum for type-safe icon specification
        var contextDemo = new MenuItem("context-demo", "Context Menu", IconKind.List, "context-demo", MenuLocation.Main, 5);
        contextDemo.ContextActions = new[]
        {
            new MenuAction { Label = "Edit", Icon = IconKind.Edit, Execute = _ => { } },
            new MenuAction { Label = "Delete", Icon = IconKind.Delete, Execute = _ => { } },
            new MenuAction { Label = "Info", Icon = IconKind.Info, Execute = _ => { } }
        };

        var lifecycleDemo = new MenuItem("lifecycle-demo", "Page Lifecycle", IconKind.Refresh, "lifecycle-demo", MenuLocation.Main, 6);
        lifecycleDemo.InstanceMode = PageInstanceMode.Transient;

        var navigationGroup = MenuItem.CreateGroup("navigation", "Navigation", IconKind.Compass, new[]
        {
            new MenuItem("basic-nav", "Basic Navigation", IconKind.Link, "basic-nav", MenuLocation.Main, 0),
            new MenuItem("badge-nav", "Badge Indicators", IconKind.Notification, "badge-nav", MenuLocation.Main, 1) 
            { 
                BadgeCount = 5, 
                BadgeColor = "error" 
            },
            new MenuItem("disabled-nav", "Disabled States", IconKind.Block, "disabled-nav", MenuLocation.Main, 2),
            MenuItem.CreateGroup("sub-menu", "Sub Menu Demo", IconKind.Folder, new[]
            {
                new MenuItem("sub-item-1", "Sub Item 1", IconKind.File, "sub-item-1", MenuLocation.Main, 0),
                new MenuItem("sub-item-2", "Sub Item 2", IconKind.File, "sub-item-2", MenuLocation.Main, 1),
                new MenuItem("sub-item-3", "Sub Item 3 (Disabled)", IconKind.File, "sub-item-3", MenuLocation.Main, 2) { IsEnabled = false }
            }, MenuLocation.Main, 3),
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
            "context-demo" => "Context Menu",
            "lifecycle-demo" => "Page Lifecycle",
            _ => "Basic Navigation"
        };
    }
}

