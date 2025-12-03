using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// Represents an item in a NavigationView control.
/// </summary>
public partial class NavigationViewItem : TemplatedControl
{
    /// <summary>
    /// Converter that adds 1 to the depth value.
    /// </summary>
    public static readonly IValueConverter DepthPlusOneConverter =
        new FuncValueConverter<int, int>(depth => depth + 1);

    private Border? _rootBorder;
    private ItemsControl? _childItemsHost;

    /// <summary>
    /// Template part name for the root border.
    /// </summary>
    public const string PART_RootBorder = "PART_RootBorder";

    /// <summary>
    /// Template part name for the child items host.
    /// </summary>
    public const string PART_ChildItemsHost = "PART_ChildItemsHost";

    static NavigationViewItem()
    {
        ItemProperty.Changed.AddClassHandler<NavigationViewItem>((x, e) => x.OnItemChanged(e));
        IsSelectedProperty.Changed.AddClassHandler<NavigationViewItem>((x, e) => x.OnIsSelectedChanged(e));
        IsExpandedProperty.Changed.AddClassHandler<NavigationViewItem>((x, e) => x.OnIsExpandedChanged(e));
        IsCollapsedProperty.Changed.AddClassHandler<NavigationViewItem>((x, e) => x.OnIsCollapsedChanged(e));
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Unsubscribe from old
        if (_rootBorder != null)
        {
            _rootBorder.PointerPressed -= OnRootPointerPressed;
        }

        _rootBorder = e.NameScope.Find<Border>(PART_RootBorder);
        _childItemsHost = e.NameScope.Find<ItemsControl>(PART_ChildItemsHost);

        // Subscribe to new
        if (_rootBorder != null)
        {
            _rootBorder.PointerPressed += OnRootPointerPressed;
        }

        UpdateVisualState();
        UpdateChildItems();
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Item == null || !Item.IsEnabled) return;

        // Find parent NavigationView
        var navView = this.FindAncestorOfType<NavigationView>();
        navView?.OnItemClicked(Item);

        e.Handled = true;
    }

    private void OnItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var item = e.NewValue as UiMenuItem;
        if (item != null)
        {
            IsExpanded = item.IsExpanded;
            IsEnabled = item.IsEnabled;
            
            // Subscribe to property changes
            item.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(UiMenuItem.IsExpanded))
                {
                    IsExpanded = item.IsExpanded;
                }
            };
        }
        UpdateVisualState();
        UpdateChildItems();
    }

    private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        PseudoClasses.Set(":selected", (bool)e.NewValue!);
    }

    private void OnIsExpandedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        PseudoClasses.Set(":expanded", (bool)e.NewValue!);
        
        // Sync back to Item
        if (Item != null && Item.IsExpanded != (bool)e.NewValue!)
        {
            Item.IsExpanded = (bool)e.NewValue!;
        }
    }

    private void OnIsCollapsedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        PseudoClasses.Set(":collapsed", (bool)e.NewValue!);
    }

    private void UpdateVisualState()
    {
        if (Item == null) return;

        PseudoClasses.Set(":haschildren", Item.Children != null && Item.Children.Count > 0);
        PseudoClasses.Set(":hasbadge", Item.BadgeCount.HasValue && Item.BadgeCount > 0);
        PseudoClasses.Set(":disabled", !Item.IsEnabled);
        PseudoClasses.Set(":expanded", IsExpanded);
        PseudoClasses.Set(":collapsed", IsCollapsed);
    }

    private void UpdateChildItems()
    {
        if (_childItemsHost != null && Item?.Children != null)
        {
            _childItemsHost.ItemsSource = Item.Children;
        }
    }
}

