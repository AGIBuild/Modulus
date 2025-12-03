using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A navigation view control that displays a hierarchical list of navigation items.
/// Supports collapsed mode, badges, disabled states, and multi-level nesting.
/// </summary>
public partial class NavigationView : TemplatedControl
{
    private ItemsControl? _itemsHost;
    private ScrollViewer? _scrollViewer;

    /// <summary>
    /// Template part name for the items host.
    /// </summary>
    public const string PART_ItemsHost = "PART_ItemsHost";

    /// <summary>
    /// Template part name for the scroll viewer.
    /// </summary>
    public const string PART_ScrollViewer = "PART_ScrollViewer";

    static NavigationView()
    {
        // Register property changed handlers
        ItemsProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnItemsChanged(e));
        SelectedItemProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnSelectedItemChanged(e));
        IsCollapsedProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnIsCollapsedChanged(e));
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _itemsHost = e.NameScope.Find<ItemsControl>(PART_ItemsHost);
        _scrollViewer = e.NameScope.Find<ScrollViewer>(PART_ScrollViewer);

        if (_itemsHost != null)
        {
            _itemsHost.ItemsSource = Items;
        }

        UpdateVisualState();
    }

    /// <summary>
    /// Called when the Items property changes.
    /// </summary>
    private void OnItemsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_itemsHost != null)
        {
            _itemsHost.ItemsSource = e.NewValue as IEnumerable;
        }
    }

    /// <summary>
    /// Called when the SelectedItem property changes.
    /// </summary>
    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var oldItem = e.OldValue as UiMenuItem;
        var newItem = e.NewValue as UiMenuItem;

        if (newItem != null)
        {
            SelectionChanged?.Invoke(this, newItem);
        }

        UpdateVisualState();
    }

    /// <summary>
    /// Called when the IsCollapsed property changes.
    /// </summary>
    private void OnIsCollapsedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        PseudoClasses.Set(":collapsed", (bool)e.NewValue!);
        UpdateVisualState();
    }

    /// <summary>
    /// Handles item click/selection.
    /// </summary>
    internal void OnItemClicked(UiMenuItem item)
    {
        if (!item.IsEnabled) return;

        // Toggle expansion for group items
        if (item.Children != null && item.Children.Count > 0)
        {
            item.IsExpanded = !item.IsExpanded;
        }

        // Navigate if has navigation key
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

    /// <summary>
    /// Updates visual state based on current properties.
    /// </summary>
    private void UpdateVisualState()
    {
        PseudoClasses.Set(":collapsed", IsCollapsed);
    }
}

