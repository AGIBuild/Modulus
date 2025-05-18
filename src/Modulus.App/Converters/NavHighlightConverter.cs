using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Modulus.App.Converters;

/// <summary>
/// Converter that returns a highlight color brush for active navigation items
/// and a transparent brush for inactive items.
/// </summary>
public class NavHighlightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            // Create gradient brush for active items
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative)
            };
            
            gradientBrush.GradientStops.Add(new GradientStop(Color.Parse("#3B82F6"), 0.0)); // Primary blue
            gradientBrush.GradientStops.Add(new GradientStop(Color.Parse("#2563EB"), 1.0)); // Slightly darker blue
            
            return gradientBrush;
        }
        
        // For inactive items, return a semi-transparent background
        return new SolidColorBrush(Color.Parse("#22FFFFFF"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}