using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.VisualTree;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// Represents an item in a NavigationView control.
/// </summary>
public partial class NavigationViewItem : TemplatedControl
{
    #region Converters

    /// <summary>
    /// Converter that adds 1 to the depth value for child items.
    /// </summary>
    public static readonly IValueConverter DepthPlusOneConverter =
        new FuncValueConverter<int, int>(depth => depth + 1);

    /// <summary>
    /// Converter that checks if item has children.
    /// </summary>
    public static readonly IValueConverter HasChildrenConverter =
        new FuncValueConverter<UiMenuItem?, bool>(item => item?.Children != null && item.Children.Count > 0);

    /// <summary>
    /// Converter that checks if badge should be visible.
    /// </summary>
    public static readonly IValueConverter HasBadgeConverter =
        new FuncValueConverter<int?, bool>(count => count.HasValue && count > 0);

    #endregion

    #region Private Fields

    private Border? _rootBorder;
    private ItemsControl? _childItemsHost;

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Unsubscribe from old
        if (_rootBorder != null)
        {
            _rootBorder.PointerPressed -= OnRootPointerPressed;
        }

        _rootBorder = e.NameScope.Find<Border>("PART_RootBorder");
        _childItemsHost = e.NameScope.Find<ItemsControl>("PART_ChildItemsHost");

        // Subscribe to new
        if (_rootBorder != null)
        {
            _rootBorder.PointerPressed += OnRootPointerPressed;
        }

        UpdateChildItems();
    }

    #endregion

    #region Event Handlers

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Item == null || !Item.IsEnabled) return;

        // Find parent NavigationView
        var navView = this.FindAncestorOfType<NavigationView>();
        navView?.OnItemClicked(Item);

        e.Handled = true;
    }

    #endregion

    #region Property Changed Handlers

    private void OnItemChanged(UiMenuItem? newItem)
    {
        if (newItem != null)
        {
            IsExpanded = newItem.IsExpanded;
            IsEnabled = newItem.IsEnabled;

            // Subscribe to property changes
            newItem.PropertyChanged += OnItemPropertyChanged;
        }

        UpdateChildItems();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
        }
    }

    private void OnIsExpandedChanged(bool newValue)
    {
        // Sync back to Item
        if (Item != null && Item.IsExpanded != newValue)
        {
            Item.IsExpanded = newValue;
        }
    }

    #endregion

    #region Private Methods

    private void UpdateChildItems()
    {
        if (_childItemsHost != null && Item?.Children != null)
        {
            _childItemsHost.ItemsSource = Item.Children;
        }
    }

    #endregion
}
