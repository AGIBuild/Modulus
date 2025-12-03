using System;
using Avalonia;
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
public class NavigationViewItem : TemplatedControl
{
    #region Converters

    /// <summary>
    /// Converter that adds 1 to the depth value for child items.
    /// </summary>
    public static readonly IValueConverter DepthPlusOneConverter =
        new FuncValueConverter<int, int>(depth => depth + 1);

    #endregion

    #region Private Fields

    private Border? _rootBorder;
    private ItemsControl? _childItemsHost;

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
        AvaloniaProperty.Register<NavigationViewItem, int>(nameof(Depth));

    /// <summary>
    /// Defines the <see cref="IsSelected"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(nameof(IsSelected));

    /// <summary>
    /// Defines the <see cref="IsExpanded"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(nameof(IsExpanded));

    /// <summary>
    /// Defines the <see cref="IsCollapsed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(nameof(IsCollapsed));

    #endregion

    #region Properties

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

    #region Constructor

    static NavigationViewItem()
    {
        ItemProperty.Changed.AddClassHandler<NavigationViewItem>((x, _) => x.OnItemChanged());
        IsSelectedProperty.Changed.AddClassHandler<NavigationViewItem>((x, _) => x.UpdatePseudoClasses());
        IsExpandedProperty.Changed.AddClassHandler<NavigationViewItem>((x, e) => x.OnIsExpandedChanged(e));
        IsCollapsedProperty.Changed.AddClassHandler<NavigationViewItem>((x, _) => x.UpdatePseudoClasses());
    }

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

        UpdatePseudoClasses();
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

    private void OnItemChanged()
    {
        if (Item != null)
        {
            IsExpanded = Item.IsExpanded;
            IsEnabled = Item.IsEnabled;

            // Subscribe to property changes
            Item.PropertyChanged += OnItemPropertyChanged;
        }
        
        UpdatePseudoClasses();
        UpdateChildItems();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (Item == null) return;

        if (e.PropertyName == nameof(UiMenuItem.IsExpanded))
        {
            IsExpanded = Item.IsExpanded;
        }
        else if (e.PropertyName == nameof(UiMenuItem.IsEnabled))
        {
            IsEnabled = Item.IsEnabled;
            UpdatePseudoClasses();
        }
        else if (e.PropertyName == nameof(UiMenuItem.BadgeCount))
        {
            UpdatePseudoClasses();
        }
    }

    private void OnIsExpandedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        UpdatePseudoClasses();

        // Sync back to Item
        if (Item != null && Item.IsExpanded != (bool)e.NewValue!)
        {
            Item.IsExpanded = (bool)e.NewValue!;
        }
    }

    #endregion

    #region Private Methods

    private void UpdatePseudoClasses()
    {
        if (Item == null) return;

        PseudoClasses.Set(":selected", IsSelected);
        PseudoClasses.Set(":expanded", IsExpanded);
        PseudoClasses.Set(":collapsed", IsCollapsed);
        PseudoClasses.Set(":haschildren", Item.Children != null && Item.Children.Count > 0);
        PseudoClasses.Set(":hasbadge", Item.BadgeCount.HasValue && Item.BadgeCount > 0);
        PseudoClasses.Set(":disabled", !Item.IsEnabled);
    }

    private void UpdateChildItems()
    {
        if (_childItemsHost != null && Item?.Children != null)
        {
            _childItemsHost.ItemsSource = Item.Children;
        }
    }

    #endregion
}
