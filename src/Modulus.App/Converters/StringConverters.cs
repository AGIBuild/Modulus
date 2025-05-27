using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Modulus.App.Converters
{
    public static class StringConverters
    {
        public static readonly IValueConverter IsNotNullOrEmpty =
            new FuncValueConverter<string?, bool>(s => !string.IsNullOrEmpty(s));

        public static readonly IValueConverter IsNullOrEmpty =
            new FuncValueConverter<string?, bool>(s => string.IsNullOrEmpty(s));
    }
}
