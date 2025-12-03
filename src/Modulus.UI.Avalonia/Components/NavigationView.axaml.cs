using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Components;

public partial class NavigationView : UserControl
{
    private static readonly IBrush DefaultBackground = Brushes.Transparent;
    private static readonly IBrush HoverBackground = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
    private static readonly IBrush SelectedBackground = new SolidColorBrush(Color.FromArgb(40, 96, 160, 255));

    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<NavigationView, IEnumerable?>(nameof(Items));

    public static readonly StyledProperty<UiMenuItem?> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationView, UiMenuItem?>(
            nameof(SelectedItem),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationView, bool>(nameof(IsCollapsed));

    /// <summary>
    /// Converter for IsEnabled to Opacity (1.0 for enabled, 0.4 for disabled).
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
                    if (s is global::Avalonia.Controls.MenuItem mi && mi.Tag is Modulus.UI.Abstractions.MenuAction ma)
                    {
                        if (mi.DataContext is UiMenuItem item)
                        {
                            ma.Execute(item);
                        }
                    }
                };
                menu.Items.Add(menuItem);
            }
            return menu;
        });

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

    /// <summary>
    /// Whether the navigation is in collapsed (icon-only) mode.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>
    /// Command executed when a nav item is clicked.
    /// </summary>
    public IRelayCommand<UiMenuItem> ItemClickCommand { get; }

    /// <summary>
    /// Event raised when an item is selected/clicked.
    /// </summary>
    public event EventHandler<UiMenuItem>? ItemSelected;

    public NavigationView()
    {
        ItemClickCommand = new RelayCommand<UiMenuItem>(OnItemClick);
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == SelectedItemProperty)
        {
            UpdateSelectionVisuals();
        }
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is UiMenuItem item)
        {
            if (item.IsEnabled)
            {
                OnItemClick(item);
            }
        }
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
        
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        // Find all Border elements and update their backgrounds
        UpdateBordersRecursive(this);
    }

    private void UpdateBordersRecursive(Control control)
    {
        if (control is Border border && border.Tag is UiMenuItem item)
        {
            var isSelected = SelectedItem?.Id == item.Id;
            border.Background = isSelected ? SelectedBackground : DefaultBackground;
        }

        if (control is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                UpdateBordersRecursive(child);
            }
        }
        else if (control is ContentControl contentControl && contentControl.Content is Control contentChild)
        {
            UpdateBordersRecursive(contentChild);
        }
        else if (control is ItemsControl itemsControl)
        {
            foreach (var child in itemsControl.GetRealizedContainers())
            {
                UpdateBordersRecursive(child);
            }
        }
        else if (control is Decorator decorator && decorator.Child != null)
        {
            UpdateBordersRecursive(decorator.Child);
        }
    }
}
