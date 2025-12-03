using System;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A navigation view control that displays a hierarchical list of navigation items.
/// Supports collapsed mode, badges, disabled states, and multi-level nesting.
/// </summary>
public class NavigationView : TemplatedControl
{
    private ItemsControl? _itemsHost;

    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Items"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<NavigationView, IEnumerable?>(nameof(Items));

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
        AvaloniaProperty.Register<NavigationView, bool>(nameof(IsCollapsed));

    /// <summary>
    /// Defines the <see cref="ItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<NavigationView, IDataTemplate?>(nameof(ItemTemplate));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the collection of menu items.
    /// </summary>
    [Content]
    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

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

    /// <summary>
    /// Gets or sets the data template for items.
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
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
        ItemsProperty.Changed.AddClassHandler<NavigationView>((x, _) => x.OnItemsChanged());
        SelectedItemProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnSelectedItemChanged(e));
        IsCollapsedProperty.Changed.AddClassHandler<NavigationView>((x, _) => x.UpdatePseudoClasses());
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _itemsHost = e.NameScope.Find<ItemsControl>("PART_ItemsHost");
        
        if (_itemsHost != null)
        {
            _itemsHost.ItemsSource = Items;
        }

        UpdatePseudoClasses();
    }

    #endregion

    #region Property Changed Handlers

    private void OnItemsChanged()
    {
        if (_itemsHost != null)
        {
            _itemsHost.ItemsSource = Items;
        }
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var newItem = e.NewValue as UiMenuItem;
        
        // Update IsSelected on all NavigationViewItems
        UpdateItemsSelection(newItem);

        if (newItem != null)
        {
            SelectionChanged?.Invoke(this, newItem);
        }

        UpdatePseudoClasses();
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Called by NavigationViewItem when clicked.
    /// </summary>
    internal void OnItemClicked(UiMenuItem item)
    {
        if (!item.IsEnabled) return;

        // Toggle expansion for group items
        if (item.Children != null && item.Children.Count > 0)
        {
            item.IsExpanded = !item.IsExpanded;
        }

        // Select item if it has a navigation key
        if (!string.IsNullOrEmpty(item.NavigationKey))
        {
            SelectedItem = item;
            ItemInvoked?.Invoke(this, item);
        }
    }

    /// <summary>
    /// Checks if the given item is currently selected.
    /// </summary>
    public bool IsItemSelected(UiMenuItem? item)
    {
        return item != null && SelectedItem?.Id == item.Id;
    }

    #endregion

    #region Private Methods

    private void UpdateItemsSelection(UiMenuItem? selectedItem)
    {
        foreach (var navItem in this.GetVisualDescendants().OfType<NavigationViewItem>())
        {
            navItem.IsSelected = navItem.Item != null &&
                                 selectedItem != null &&
                                 navItem.Item.Id == selectedItem.Id;
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":collapsed", IsCollapsed);
    }

    #endregion
}
