using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Modulus.App.Controls.ViewModels;

/// <summary>
/// ViewModel for the NavigationView control that combines navigation bar and content area.
/// </summary>
public partial class NavigationViewModel : ObservableObject
{
    private readonly INavigationService? _navigationService;
    
    #region Properties

    /// <summary>
    /// 是否显示Header部分
    /// </summary>
    [ObservableProperty]
    private bool showHeader = true;
    
    /// <summary>
    /// 是否显示Body部分
    /// </summary>
    [ObservableProperty]
    private bool showBody = true;
    
    /// <summary>
    /// 是否显示Footer部分
    /// </summary>
    [ObservableProperty]
    private bool showFooter = true;
    
    /// <summary>
    /// 应用名称
    /// </summary>
    [ObservableProperty]
    private string appName = "Modulus";
    
    /// <summary>
    /// 应用版本
    /// </summary>
    [ObservableProperty]
    private string appVersion = "v1.0.0";
    
    /// <summary>
    /// 应用图标路径
    /// </summary>
    [ObservableProperty]
    private string appIcon = "avares://Modulus.App/Assets/avalonia-logo.ico";

    /// <summary>
    /// The width of the navigation bar, which changes when expanded/collapsed
    /// </summary>
    [ObservableProperty]
    private GridLength navigationBarWidth = new GridLength(40);

    /// <summary>
    /// Whether the navigation bar is currently expanded
    /// </summary>
    [ObservableProperty]
    private bool isNavigationExpanded;

    /// <summary>
    /// Whether the navigation is in overlay mode (used on smaller screens)
    /// </summary>
    [ObservableProperty]
    private bool isNavigationOverlayed;

    /// <summary>
    /// Icon to display on the collapse/expand button
    /// </summary>
    [ObservableProperty]
    private string collapseExpandIcon = "\uE700"; // Menu icon (hamburger)

    /// <summary>
    /// Background color for the content area
    /// </summary>
    [ObservableProperty]
    private IBrush contentBackground = new SolidColorBrush(Color.Parse("#F9FAFB"));

    /// <summary>
    /// The current content page being displayed
    /// </summary>
    [ObservableProperty]
    private object? currentPage;

    /// <summary>
    /// The title of the current page
    /// </summary>
    [ObservableProperty]
    private string currentPageTitle = "Dashboard";
    
    /// <summary>
    /// 主导航项集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationItemModel> navigationItems = new();
    
    /// <summary>
    /// 页脚导航项集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationItemModel> footerItems = new();
    
    /// <summary>
    /// 导航到指定视图的命令
    /// </summary>
    [ObservableProperty]
    private IRelayCommand<NavigationItemModel>? navigateToViewCommand;

    /// <summary>
    /// 状态消息（用于显示插件加载等状态）
    /// </summary>
    [ObservableProperty]
    private string statusMessage = string.Empty;

    #endregion

    #region Commands

    /// <summary>
    /// Command to toggle the navigation bar between expanded and collapsed states
    /// </summary>
    [RelayCommand]
    private void ToggleNavigationBar()
    {
        IsNavigationExpanded = !IsNavigationExpanded;
        
        if (IsNavigationExpanded)
        {
            NavigationBarWidth = new GridLength(220);
            CollapseExpandIcon = "\uE700"; // ChevronLeft icon
        }
        else
        {
            NavigationBarWidth = new GridLength(40);
            CollapseExpandIcon = "\uE700"; // Menu icon (hamburger)
        }
    }
    
    /// <summary>
    /// 导航到指定页面命令（内部实现）
    /// </summary>
    [RelayCommand]
    private void NavigateToViewInternal(NavigationItemModel item)
    {
        if (item != null && !string.IsNullOrEmpty(item.ViewName))
        {
            _navigationService?.NavigateTo(item.ViewName, item.Parameter);
        }
    }

    #endregion
    
    /// <summary>
    /// 添加导航项
    /// </summary>
    public void AddNavigationItem(NavigationItemModel item)
    {
        if (item.Section.Equals("footer", StringComparison.OrdinalIgnoreCase))
        {
            FooterItems.Add(item);
        }
        else
        {
            NavigationItems.Add(item);
        }
    }
    
    /// <summary>
    /// 快速创建并添加导航项
    /// </summary>
    public NavigationItemModel AddNavigationItem(string label, string icon, string viewName, string section = "body", object? parameter = null)
    {
        var item = new NavigationItemModel
        {
            Label = label,
            Icon = icon,
            ViewName = viewName,
            Section = section,
            Parameter = parameter
        };
        
        AddNavigationItem(item);
        return item;
    }

    /// <summary>
    /// Updates the layout based on screen size
    /// </summary>
    /// <param name="width">The new width of the control</param>
    public void UpdateLayout(double width)
    {
        // If width is less than 1024px, use overlay mode
        IsNavigationOverlayed = width < 1024;
        
        // Auto-collapse on very small screens
        if (width < 640 && IsNavigationExpanded)
        {
            ToggleNavigationBar();
        }
    }
    
    /// <summary>
    /// Activates the specified navigation item and deactivates others
    /// </summary>
    public void SetActiveNavigationItem(string viewName)
    {
        // 清除所有导航项的活动状态
        foreach (var item in NavigationItems)
        {
            item.IsActive = item.ViewName == viewName;
        }
        
        foreach (var item in FooterItems)
        {
            item.IsActive = item.ViewName == viewName;
        }
    }
    
    /// <summary>
    /// Constructor that initializes the NavigationViewModel
    /// </summary>
    public NavigationViewModel(INavigationService? navigationService = null)
    {
        _navigationService = navigationService;
        
        // 默认使用内部导航命令
        if (_navigationService != null)
        {
            NavigateToViewCommand = NavigateToViewInternalCommand;
        }
    }
    
    /// <summary>
    /// 获取指定视图名称的导航项
    /// </summary>
    public NavigationItemModel? GetNavigationItem(string viewName)
    {
        return NavigationItems.Concat(FooterItems)
            .FirstOrDefault(item => item.ViewName == viewName);
    }
}

/// <summary>
/// Contains information about a navigation destination page
/// </summary>
public class NavigationPageInfo
{
    /// <summary>
    /// The page content or view model
    /// </summary>
    public object? Page { get; set; }
    
    /// <summary>
    /// The title of the page to display in the header
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Navigation route/identifier for this page
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// Icon character to display (typically from an icon font like Segoe MDL2 Assets)
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}
