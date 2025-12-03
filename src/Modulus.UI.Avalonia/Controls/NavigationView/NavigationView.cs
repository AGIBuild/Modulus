using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A navigation view control that displays a hierarchical list of navigation items.
/// Supports collapsed mode, badges, disabled states, and multi-level nesting.
/// </summary>
public partial class NavigationView : TemplatedControl
{
    private ItemsControl? _itemsHost;

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

    #endregion

    #region Property Changed Handlers

    private void OnItemsChanged(IEnumerable? newItems)
    {
        if (_itemsHost != null)
        {
            _itemsHost.ItemsSource = newItems;
        }
    }

    private void OnSelectedItemChanged(UiMenuItem? newItem)
    {
        // Update IsSelected on all NavigationViewItems
        foreach (var navItem in this.GetVisualDescendants().OfType<NavigationViewItem>())
        {
            navItem.IsSelected = navItem.Item != null &&
                                 newItem != null &&
                                 navItem.Item.Id == newItem.Id;
        }

        if (newItem != null)
        {
            SelectionChanged?.Invoke(this, newItem);
        }
    }

    #endregion
}
