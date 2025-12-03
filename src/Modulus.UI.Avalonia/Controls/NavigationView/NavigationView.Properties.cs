using System.Collections;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

public partial class NavigationView : TemplatedControl
{
    #region Items Property

    /// <summary>
    /// Defines the <see cref="Items"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<NavigationView, IEnumerable?>(nameof(Items));

    /// <summary>
    /// Gets or sets the collection of menu items.
    /// </summary>
    [Content]
    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    #endregion

    #region SelectedItem Property

    /// <summary>
    /// Defines the <see cref="SelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<UiMenuItem?> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationView, UiMenuItem?>(
            nameof(SelectedItem),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the currently selected menu item.
    /// </summary>
    public UiMenuItem? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    #endregion

    #region IsCollapsed Property

    /// <summary>
    /// Defines the <see cref="IsCollapsed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationView, bool>(nameof(IsCollapsed), defaultValue: false);

    /// <summary>
    /// Gets or sets whether the navigation is in collapsed (icon-only) mode.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion

    #region ItemTemplate Property

    /// <summary>
    /// Defines the <see cref="ItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<NavigationView, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// Gets or sets the data template for items.
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    #endregion

    #region Static Constructor

    static NavigationView()
    {
        ItemsProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnItemsChanged(e.NewValue as IEnumerable));
        SelectedItemProperty.Changed.AddClassHandler<NavigationView>((x, e) => x.OnSelectedItemChanged(e.NewValue as UiMenuItem));
    }

    #endregion
}

