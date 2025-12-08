using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Modulus.UI.Avalonia.Behaviors;

/// <summary>
/// Behavior that enables window dragging from any attached control.
/// </summary>
public class WindowDragBehavior : Behavior<Control>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed += OnPointerPressed;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed -= OnPointerPressed;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (AssociatedObject == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(AssociatedObject);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (TopLevel.GetTopLevel(AssociatedObject) is Window window)
        {
            if (e.ClickCount >= 2)
            {
                if (window.CanResize)
                {
                    window.WindowState = window.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
                e.Handled = true;
                return;
            }

            window.BeginMoveDrag(e);
        }
    }
}

