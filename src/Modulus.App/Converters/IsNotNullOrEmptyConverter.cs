using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Modulus.App.Converters
{
    public class IsNotNullOrEmptyConverter : IValueConverter
    {
        public static readonly IsNotNullOrEmptyConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
                
            if (value is string str)
                return !string.IsNullOrEmpty(str);
                
            if (value is System.Collections.IEnumerable enumerable)
                return enumerable.GetEnumerator().MoveNext();
                
            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
