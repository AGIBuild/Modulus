using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Components;

/// <summary>
/// A themed navigation view component supporting hierarchical menus,
/// badges, disabled states, and selection highlighting.
/// </summary>
public partial class NavigationView : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<NavigationView, IEnumerable?>(nameof(Items));

    public static readonly StyledProperty<UiMenuItem?> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationView, UiMenuItem?>(
            nameof(SelectedItem),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationView, bool>(nameof(IsCollapsed));

    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public UiMenuItem? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion

    #region Converters

    /// <summary>
    /// Converter for IsEnabled to Opacity.
    /// </summary>
    public static readonly IValueConverter EnabledToOpacityConverter =
        new FuncValueConverter<bool, double>(isEnabled => isEnabled ? 1.0 : 0.4);

    /// <summary>
    /// Converter for IsExpanded to chevron character.
    /// </summary>
    public static readonly IValueConverter ExpandedToChevronConverter =
        new FuncValueConverter<bool, string>(isExpanded => isExpanded ? "▼" : "▶");

    /// <summary>
    /// Converter for ContextActions to ContextMenu.
    /// </summary>
    public static readonly IValueConverter ActionsToContextMenuConverter =
        new FuncValueConverter<IReadOnlyList<Modulus.UI.Abstractions.MenuAction>?, ContextMenu?>(actions =>
        {
            if (actions == null || actions.Count == 0) return null;

            var menu = new ContextMenu();
            foreach (var action in actions)
            {
                var menuItem = new global::Avalonia.Controls.MenuItem
                {
                    Header = action.Label,
                    Tag = action
                };
                menuItem.Click += (s, e) =>
                {
                    if (s is global::Avalonia.Controls.MenuItem mi && 
                        mi.Tag is Modulus.UI.Abstractions.MenuAction ma &&
                        mi.DataContext is UiMenuItem item)
                    {
                        ma.Execute(item);
                    }
                };
                menu.Items.Add(menuItem);
            }
            return menu;
        });

    #endregion

    #region Events

    /// <summary>
    /// Command executed when a nav item is clicked.
    /// </summary>
    public IRelayCommand<UiMenuItem> ItemClickCommand { get; }

    /// <summary>
    /// Event raised when an item is selected/clicked.
    /// </summary>
    public event EventHandler<UiMenuItem>? ItemSelected;

    #endregion

    private Border? _selectedBorder;

    public NavigationView()
    {
        ItemClickCommand = new RelayCommand<UiMenuItem>(OnItemClick);
        InitializeComponent();
        
        // Subscribe to pointer events for nav items
        AddHandler(PointerPressedEvent, OnPointerPressedTunneled, handledEventsToo: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == SelectedItemProperty)
        {
            UpdateSelectedClass(change.OldValue as UiMenuItem, change.NewValue as UiMenuItem);
        }
    }

    private void OnPointerPressedTunneled(object? sender, PointerPressedEventArgs e)
    {
        // Find the nav item border that was clicked
        if (e.Source is Control source)
        {
            var border = FindParentNavItem(source);
            if (border?.Tag is UiMenuItem item && item.IsEnabled)
            {
                OnItemClick(item);
                e.Handled = true;
            }
        }
    }

    private Border? FindParentNavItem(Control? control)
    {
        while (control != null)
        {
            if (control is Border border && border.Classes.Contains("navItem"))
            {
                return border;
            }
            control = control.Parent as Control;
        }
        return null;
    }

    private void OnItemClick(UiMenuItem? item)
    {
        if (item == null || !item.IsEnabled) return;

        // If item has children, toggle expansion
        if (item.Children != null && item.Children.Count > 0)
        {
            item.IsExpanded = !item.IsExpanded;

            // If item also has a NavigationKey, navigate
            if (!string.IsNullOrEmpty(item.NavigationKey))
            {
                SelectedItem = item;
                ItemSelected?.Invoke(this, item);
            }
        }
        else
        {
            // Leaf item - select and navigate
            SelectedItem = item;
            ItemSelected?.Invoke(this, item);
        }
    }

    private void UpdateSelectedClass(UiMenuItem? oldItem, UiMenuItem? newItem)
    {
        // Remove 'selected' class from old
        if (_selectedBorder != null)
        {
            _selectedBorder.Classes.Remove("selected");
            _selectedBorder = null;
        }

        // Add 'selected' class to new
        if (newItem != null)
        {
            _selectedBorder = FindBorderForItem(this, newItem);
            if (_selectedBorder != null)
            {
                _selectedBorder.Classes.Add("selected");
            }
        }
    }

    private Border? FindBorderForItem(Visual parent, UiMenuItem item)
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is Border border && 
                border.Classes.Contains("navItem") && 
                border.Tag is UiMenuItem menuItem && 
                menuItem.Id == item.Id)
            {
                return border;
            }

            if (child is Visual visualChild)
            {
                var found = FindBorderForItem(visualChild, item);
                if (found != null) return found;
            }
        }
        return null;
    }
}
