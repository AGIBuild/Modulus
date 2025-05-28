using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using Avalonia.Controls;

namespace Modulus.App.Converters
{
    /// <summary>
    /// Converts resource keys to PathGeometry instances for use with PathIcon
    /// </summary>
    public class ResourceKeyToGeometryConverter : IValueConverter
    {
        public static readonly ResourceKeyToGeometryConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string resourceKey && !string.IsNullOrEmpty(resourceKey))
            {
                // Try to find resource from the current visual tree first
                if (parameter is StyledElement element && 
                    element.TryFindResource(resourceKey, out var resource) && 
                    resource is PathGeometry geometry)
                {
                    return geometry;
                }

                // Fallback to application resources if we didn't find in the visual tree
                if (Application.Current?.TryFindResource(resourceKey, out var appResource) == true && 
                    appResource is PathGeometry appGeometry)
                {
                    return appGeometry;
                }
            }

            // Return an empty path instead of null to avoid binding errors
            return new PathGeometry();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}