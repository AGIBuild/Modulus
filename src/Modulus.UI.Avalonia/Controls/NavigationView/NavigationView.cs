using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A navigation view control that displays a hierarchical list of navigation items.
/// Inherits from ItemsControl for native Avalonia data binding support.
/// </summary>
public class NavigationView : ItemsControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="SelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<UiMenuItem?> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationView, UiMenuItem?>(
            nameof(SelectedItem),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="IsCollapsed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationView, bool>(nameof(IsCollapsed), defaultValue: false);

    /// <summary>
    /// Gets or sets the currently selected menu item.
    /// </summary>
    public UiMenuItem? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the navigation is in collapsed (icon-only) mode.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a navigation item is selected.
    /// </summary>
    public event EventHandler<UiMenuItem>? SelectionChanged;

    /// <summary>
    /// Raised when a navigation item is invoked (clicked).
    /// </summary>
    public event EventHandler<UiMenuItem>? ItemInvoked;

    #endregion

    #region Constructor

    static NavigationView()
    {
        SelectedItemProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnSelectedItemChanged(e));
        IsCollapsedProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnIsCollapsedChanged(e));
    }

    #endregion

    #region ItemsControl Overrides

    /// <summary>
    /// Determines if the item is its own container.
    /// </summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not NavigationViewItem;
    }

    /// <summary>
    /// Creates a container for the item.
    /// </summary>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new NavigationViewItem();
    }

    /// <summary>
    /// Prepares the container for the item.
    /// </summary>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is NavigationViewItem navItem)
        {
            // Set initial properties
            navItem.ParentNavigationView = this;
            navItem.Depth = 0;

            // Bind to MenuItem data
            if (item is UiMenuItem menuItem)
            {
                navItem.Item = menuItem;
                navItem.IsExpanded = menuItem.IsExpanded;
                navItem.IsEnabled = menuItem.IsEnabled;
                
                // Update selection state
                navItem.IsSelected = SelectedItem != null && SelectedItem.Id == menuItem.Id;
            }
        }
    }

    /// <summary>
    /// Clears the container when recycled.
    /// </summary>
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);

        if (container is NavigationViewItem navItem)
        {
            navItem.ParentNavigationView = null;
            navItem.Item = null;
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Called by NavigationViewItem when clicked.
    /// </summary>
    internal void OnItemClicked(NavigationViewItem navItem, UiMenuItem menuItem)
    {
        if (!menuItem.IsEnabled) return;

        // Toggle expansion for group items
        if (menuItem.Children != null && menuItem.Children.Count > 0)
        {
            menuItem.IsExpanded = !menuItem.IsExpanded;
            navItem.IsExpanded = menuItem.IsExpanded;
        }

        // Raise ItemInvoked for all clicks
        ItemInvoked?.Invoke(this, menuItem);

        // Select item if it has a navigation key (leaf items)
        if (!string.IsNullOrEmpty(menuItem.NavigationKey))
        {
            SelectedItem = menuItem;
        }
    }

    /// <summary>
    /// Updates selection state on all NavigationViewItems.
    /// </summary>
    internal void UpdateSelectionState(UiMenuItem? selectedItem)
    {
        // Use visual tree traversal for Avalonia 11
        foreach (var navItem in this.GetVisualDescendants().OfType<NavigationViewItem>())
        {
            navItem.IsSelected = navItem.Item != null && 
                                 selectedItem != null && 
                                 navItem.Item.Id == selectedItem.Id;
        }
    }

    #endregion

    #region Property Changed Handlers

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var newItem = e.NewValue as UiMenuItem;
        UpdateSelectionState(newItem);

        if (newItem != null)
        {
            SelectionChanged?.Invoke(this, newItem);
        }
    }

    private void OnIsCollapsedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Update all child items using visual tree traversal
        foreach (var navItem in this.GetVisualDescendants().OfType<NavigationViewItem>())
        {
            navItem.IsCollapsed = (bool)e.NewValue!;
        }
    }

    #endregion
}
