using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Avalonia.Components;

public partial class NavigationView : UserControl
{
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
                        // Get the MenuItem from DataContext
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
    public ICommand ItemClickCommand { get; }

    /// <summary>
    /// Event raised when an item is selected/clicked.
    /// </summary>
    public event EventHandler<UiMenuItem>? ItemSelected;

    private List<UiMenuItem> _flatItems = new();
    private int _focusedIndex = -1;

    public NavigationView()
    {
        ItemClickCommand = new RelayCommand<UiMenuItem>(OnItemClick);
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsProperty)
        {
            UpdateFlatItemsList();
        }
    }

    private void UpdateFlatItemsList()
    {
        _flatItems.Clear();
        if (Items == null) return;

        foreach (var item in Items.Cast<UiMenuItem>())
        {
            _flatItems.Add(item);
            if (item.Children != null && item.IsExpanded)
            {
                _flatItems.AddRange(item.Children);
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
            UpdateFlatItemsList();

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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_flatItems.Count == 0)
        {
            UpdateFlatItemsList();
        }

        switch (e.Key)
        {
            case Key.Up:
                MoveFocus(-1);
                e.Handled = true;
                break;

            case Key.Down:
                MoveFocus(1);
                e.Handled = true;
                break;

            case Key.Enter:
            case Key.Space:
                ActivateFocusedItem();
                e.Handled = true;
                break;

            case Key.Right:
                ExpandFocusedGroup();
                e.Handled = true;
                break;

            case Key.Left:
                CollapseFocusedGroup();
                e.Handled = true;
                break;

            case Key.Escape:
                CollapseAllGroups();
                e.Handled = true;
                break;
        }
    }

    private void MoveFocus(int delta)
    {
        if (_flatItems.Count == 0) return;

        var newIndex = _focusedIndex + delta;
        
        // Wrap around
        if (newIndex < 0) newIndex = _flatItems.Count - 1;
        if (newIndex >= _flatItems.Count) newIndex = 0;

        // Skip disabled items
        var attempts = 0;
        while (!_flatItems[newIndex].IsEnabled && attempts < _flatItems.Count)
        {
            newIndex = (newIndex + delta + _flatItems.Count) % _flatItems.Count;
            attempts++;
        }

        _focusedIndex = newIndex;
        SelectedItem = _flatItems[_focusedIndex];
    }

    private void ActivateFocusedItem()
    {
        if (_focusedIndex >= 0 && _focusedIndex < _flatItems.Count)
        {
            OnItemClick(_flatItems[_focusedIndex]);
        }
        else if (SelectedItem != null)
        {
            OnItemClick(SelectedItem);
        }
    }

    private void ExpandFocusedGroup()
    {
        var item = _focusedIndex >= 0 && _focusedIndex < _flatItems.Count
            ? _flatItems[_focusedIndex]
            : SelectedItem;

        if (item?.Children != null && !item.IsExpanded)
        {
            item.IsExpanded = true;
            UpdateFlatItemsList();
        }
    }

    private void CollapseFocusedGroup()
    {
        var item = _focusedIndex >= 0 && _focusedIndex < _flatItems.Count
            ? _flatItems[_focusedIndex]
            : SelectedItem;

        if (item?.Children != null && item.IsExpanded)
        {
            item.IsExpanded = false;
            UpdateFlatItemsList();
        }
    }

    private void CollapseAllGroups()
    {
        if (Items == null) return;

        foreach (var item in Items.Cast<UiMenuItem>())
        {
            if (item.Children != null)
            {
                item.IsExpanded = false;
            }
        }
        UpdateFlatItemsList();
    }

    /// <summary>
    /// Simple relay command implementation.
    /// </summary>
    private class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;

        public RelayCommand(Action<T?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
