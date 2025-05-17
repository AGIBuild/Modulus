using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Modulus.App.Controls.ViewModels;

/// <summary>
/// Abstract ViewModel for the NavigationBar control that provides data binding 
/// properties for the Header, Body, and Footer sections.
/// </summary>
public partial class NavigationBarViewModel : ObservableObject
{
    /// <summary>
    /// Items to display in the header section of the navigation bar.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationMenuItemViewModel> headerItems = new();

    /// <summary>
    /// Items to display in the body section of the navigation bar.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationMenuItemViewModel> bodyItems = new();

    /// <summary>
    /// Items to display in the footer section of the navigation bar.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationMenuItemViewModel> footerItems = new();

    /// <summary>
    /// For backward compatibility with existing bindings.
    /// </summary>
    public ObservableCollection<NavigationMenuItemViewModel> MenuItems => BodyItems;

    public NavigationBarViewModel() { }
}

/// <summary>
/// ViewModel for navigation menu items that can be used in any section of the NavigationBar.
/// </summary>
public partial class NavigationMenuItemViewModel : ObservableObject
{
    /// <summary>
    /// Icon character to display (typically from an icon font like Segoe MDL2 Assets)
    /// </summary>
    [ObservableProperty]
    private string icon = string.Empty;

    /// <summary>
    /// Tooltip text to display when hovering over the menu item
    /// </summary>
    [ObservableProperty]
    private string tooltip = string.Empty;

    /// <summary>
    /// Command to execute when the menu item is clicked
    /// </summary>
    [ObservableProperty]
    private IRelayCommand? command;

    /// <summary>
    /// Whether this menu item is currently active/selected
    /// </summary>
    [ObservableProperty]
    private bool isActive;

    /// <summary>
    /// Background color for the menu item when not active
    /// </summary>
    [ObservableProperty]
    private string background = "Transparent";

    /// <summary>
    /// Width of the menu item button
    /// </summary>
    [ObservableProperty]
    private double width = 56;

    /// <summary>
    /// Height of the menu item button
    /// </summary>
    [ObservableProperty]
    private double height = 56;

    /// <summary>
    /// Corner radius of the menu item button
    /// </summary>
    [ObservableProperty]
    private double cornerRadius = 12;

    /// <summary>
    /// Font size for the icon
    /// </summary>
    [ObservableProperty]
    private double fontSize = 26;
}
