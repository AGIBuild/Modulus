using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Modulus.App.Converters;

public class NavHighlightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.Parse("#3B82F6")); // Accent blue
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}