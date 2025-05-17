using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Media;

namespace Modulus.App.Converters;

public class NavActiveToBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.Parse("#23272E")).ToImmutable(); // Active: slightly lighter or accent
        return Brushes.Transparent;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
