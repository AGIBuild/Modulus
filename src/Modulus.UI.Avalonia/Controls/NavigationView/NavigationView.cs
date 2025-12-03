using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.VisualTree;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A navigation view control that displays a hierarchical list of navigation items.
/// Supports collapsed mode, badges, disabled states, and multi-level nesting.
/// </summary>
public partial class NavigationView : TemplatedControl
{
    private static bool _stylesLoaded;
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

    public NavigationView()
    {
        // Ensure styles are loaded
        EnsureStylesLoaded();
    }

    /// <summary>
    /// Ensures that the control styles are loaded into the application.
    /// This is necessary for modules that may not have access to the main app's styles.
    /// </summary>
    private static void EnsureStylesLoaded()
    {
        if (_stylesLoaded || Application.Current == null) return;

        var styles = Application.Current.Styles;
        
        // Check if our styles are already loaded
        var genericStyleUri = new Uri("avares://Modulus.UI.Avalonia/Themes/Generic.axaml");
        bool hasGenericStyle = false;
        
        foreach (var style in styles)
        {
            if (style is StyleInclude si && si.Source == genericStyleUri)
            {
                hasGenericStyle = true;
                break;
            }
        }

        if (!hasGenericStyle)
        {
            try
            {
                var genericStyles = new StyleInclude(genericStyleUri) { Source = genericStyleUri };
                styles.Add(genericStyles);
            }
            catch
            {
                // Styles might already be loaded or unavailable
            }
        }

        _stylesLoaded = true;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _itemsHost = e.NameScope.Find<ItemsControl>(PART_ItemsHost);
        _scrollViewer = e.NameScope.Find<ScrollViewer>(PART_ScrollViewer);

        // Set ItemsSource after template is applied
        if (_itemsHost != null && Items != null)
        {
            _itemsHost.ItemsSource = Items;
        }

        UpdateVisualState();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Ensure items are set when control is attached to visual tree
        if (_itemsHost != null && Items != null)
        {
            _itemsHost.ItemsSource = Items;
        }
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

        // Update IsSelected on all NavigationViewItems
        UpdateItemsSelection(newItem);

        if (newItem != null)
        {
            SelectionChanged?.Invoke(this, newItem);
        }

        UpdateVisualState();
    }

    /// <summary>
    /// Updates the IsSelected property on all NavigationViewItems.
    /// </summary>
    private void UpdateItemsSelection(UiMenuItem? selectedItem)
    {
        foreach (var navItem in this.GetVisualDescendants().OfType<NavigationViewItem>())
        {
            navItem.IsSelected = navItem.Item != null && 
                                 selectedItem != null && 
                                 navItem.Item.Id == selectedItem.Id;
        }
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

