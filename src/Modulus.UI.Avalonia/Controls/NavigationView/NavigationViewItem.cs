using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Icons;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// Represents an item in a NavigationView control.
/// Inherits from HeaderedItemsControl to support hierarchical child menus.
/// </summary>
public class NavigationViewItem : HeaderedItemsControl
{
    protected override Type StyleKeyOverride => typeof(NavigationViewItem);

    #region Static Converters

    /// <summary>
    /// Converter that checks if badge should be visible.
    /// </summary>
    public static readonly IValueConverter HasBadgeConverter =
        new FuncValueConverter<int?, bool>(count => count.HasValue && count > 0);

    /// <summary>
    /// Converter that transforms IconKind to Geometry for rendering.
    /// </summary>
    public static readonly IValueConverter IconToGeometryConverter = IconHelper.IconKindToGeometryConverter;

    #endregion

    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Item"/> property.
    /// </summary>
    public static readonly StyledProperty<UiMenuItem?> ItemProperty =
        AvaloniaProperty.Register<NavigationViewItem, UiMenuItem?>(nameof(Item));

    /// <summary>
    /// Defines the <see cref="Depth"/> property.
    /// </summary>
    public static readonly StyledProperty<int> DepthProperty =
        AvaloniaProperty.Register<NavigationViewItem, int>(nameof(Depth), defaultValue: 0);

    /// <summary>
    /// Defines the <see cref="IsSelected"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(nameof(IsSelected), defaultValue: false);

    /// <summary>
    /// Defines the <see cref="IsExpanded"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(nameof(IsExpanded), defaultValue: false);

    /// <summary>
    /// Defines the <see cref="IsCollapsed"/> property (inherited from parent NavigationView).
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(nameof(IsCollapsed), defaultValue: false);

    /// <summary>
    /// Gets or sets the menu item data.
    /// </summary>
    public UiMenuItem? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the nesting depth of this item.
    /// </summary>
    public int Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this item's children are expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the parent NavigationView is collapsed.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion

    #region Internal Properties

    /// <summary>
    /// Reference to the parent NavigationView.
    /// </summary>
    internal NavigationView? ParentNavigationView { get; set; }

    #endregion

    #region Private Fields

    private Border? _rootBorder;
    private ContextMenu? _contextMenu;

    #endregion

    #region Constructor

    static NavigationViewItem()
    {
        ItemProperty.Changed.AddClassHandler<NavigationViewItem>((x, e) => x.OnItemChanged(e));
        IsExpandedProperty.Changed.AddClassHandler<NavigationViewItem>((x, e) => x.OnIsExpandedChanged(e));
    }

    #endregion

    #region HeaderedItemsControl Overrides

    /// <summary>
    /// Determines if the item is its own container.
    /// </summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not NavigationViewItem;
    }

    /// <summary>
    /// Creates a container for child items.
    /// </summary>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new NavigationViewItem();
    }

    /// <summary>
    /// Prepares the container for child items.
    /// </summary>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is NavigationViewItem childNavItem)
        {
            // Pass down parent reference and depth
            childNavItem.ParentNavigationView = ParentNavigationView;
            childNavItem.Depth = Depth + 1;
            childNavItem.IsCollapsed = IsCollapsed;

            // Bind to MenuItem data
            if (item is UiMenuItem menuItem)
            {
                childNavItem.Item = menuItem;
                childNavItem.IsExpanded = menuItem.IsExpanded;
                childNavItem.IsEnabled = menuItem.IsEnabled;

                // Update selection state
                if (ParentNavigationView != null)
                {
                    childNavItem.IsSelected = ParentNavigationView.SelectedItem?.Id == menuItem.Id;
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Unsubscribe from old
        if (_rootBorder != null)
        {
            _rootBorder.PointerPressed -= OnRootPointerPressed;
            _rootBorder.PointerReleased -= OnRootPointerReleased;
        }

        _rootBorder = e.NameScope.Find<Border>("PART_RootBorder");

        // Subscribe to new
        if (_rootBorder != null)
        {
            _rootBorder.PointerPressed += OnRootPointerPressed;
            _rootBorder.PointerReleased += OnRootPointerReleased;
        }

        // Set ItemsSource from Item.Children
        UpdateChildItems();
        
        // Build context menu if needed
        UpdateContextMenu();
    }

    #endregion

    #region Event Handlers

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Item == null || !Item.IsEnabled) return;

        var props = e.GetCurrentPoint(this).Properties;
        
        // Handle right-click for context menu
        if (props.IsRightButtonPressed && _contextMenu != null)
        {
            _contextMenu.Open(this);
            e.Handled = true;
            return;
        }

        // Handle left-click for selection/expansion
        if (props.IsLeftButtonPressed)
        {
            ParentNavigationView?.OnItemClicked(this, Item);
            e.Handled = true;
        }
    }

    private void OnRootPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Additional handling if needed
    }

    #endregion

    #region Property Changed Handlers

    private void OnItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var newItem = e.NewValue as UiMenuItem;

        if (newItem != null)
        {
            // Sync properties
            IsExpanded = newItem.IsExpanded;
            IsEnabled = newItem.IsEnabled;

            // Subscribe to property changes
            newItem.PropertyChanged -= OnMenuItemPropertyChanged;
            newItem.PropertyChanged += OnMenuItemPropertyChanged;
        }

        // Update child items
        UpdateChildItems();
        
        // Update context menu
        UpdateContextMenu();
    }

    private void OnMenuItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (Item == null) return;

        switch (e.PropertyName)
        {
            case nameof(UiMenuItem.IsExpanded):
                IsExpanded = Item.IsExpanded;
                break;
            case nameof(UiMenuItem.IsEnabled):
                IsEnabled = Item.IsEnabled;
                break;
            case nameof(UiMenuItem.BadgeCount):
                // Badge is bound directly in template, but we can force re-render if needed
                break;
        }
    }

    private void OnIsExpandedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Sync back to Item
        if (Item != null && Item.IsExpanded != (bool)e.NewValue!)
        {
            Item.IsExpanded = (bool)e.NewValue!;
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Updates selection state recursively for this item and all children.
    /// </summary>
    internal void UpdateSelectionRecursive(UiMenuItem? selectedItem)
    {
        IsSelected = Item != null && selectedItem != null && Item.Id == selectedItem.Id;

        foreach (var childNavItem in this.GetVisualDescendants().OfType<NavigationViewItem>())
        {
            childNavItem.UpdateSelectionRecursive(selectedItem);
        }
    }

    /// <summary>
    /// Updates collapsed state recursively for this item and all children.
    /// </summary>
    internal void UpdateCollapsedStateRecursive(bool isCollapsed)
    {
        IsCollapsed = isCollapsed;

        foreach (var childNavItem in this.GetVisualDescendants().OfType<NavigationViewItem>())
        {
            childNavItem.UpdateCollapsedStateRecursive(isCollapsed);
        }
    }

    #endregion

    #region Private Methods

    private void UpdateChildItems()
    {
        // Set ItemsSource to Children collection
        if (Item?.Children != null && Item.Children.Count > 0)
        {
            ItemsSource = Item.Children;
        }
        else
        {
            ItemsSource = null;
        }
    }

    private void UpdateContextMenu()
    {
        // Clear existing context menu
        _contextMenu = null;

        if (Item?.ContextActions == null || Item.ContextActions.Count == 0)
            return;

        // Build context menu from actions
        var contextMenu = new ContextMenu();
        var menuItems = new List<global::Avalonia.Controls.MenuItem>();

        foreach (var action in Item.ContextActions)
        {
            var menuItem = new global::Avalonia.Controls.MenuItem
            {
                Header = action.Label
            };

            // Add icon if available
            var iconGeometry = IconHelper.GetGeometry(action.Icon);
            if (iconGeometry != null)
            {
                menuItem.Icon = new global::Avalonia.Controls.Shapes.Path
                {
                    Data = iconGeometry,
                    Width = 16,
                    Height = 16,
                    Stretch = global::Avalonia.Media.Stretch.Uniform,
                    Fill = global::Avalonia.Media.Brushes.Gray
                };
            }

            // Capture for closure
            var capturedAction = action;
            var capturedItem = Item;
            menuItem.Click += (s, e) => capturedAction.Execute?.Invoke(capturedItem);

            menuItems.Add(menuItem);
        }

        contextMenu.ItemsSource = menuItems;
        _contextMenu = contextMenu;
    }

    #endregion
}
