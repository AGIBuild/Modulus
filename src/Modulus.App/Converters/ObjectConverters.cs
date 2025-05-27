using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Modulus.App.Converters
{
    public static class ObjectConverters
    {
        public static readonly IValueConverter IsNotNull =
            new FuncValueConverter<object?, bool>(val => val != null);

        public static readonly IValueConverter IsNull =
            new FuncValueConverter<object?, bool>(val => val == null);

        public static readonly IValueConverter IsNotNullOrEmpty =
            new FuncValueConverter<IEnumerable?, bool>(items => items?.Cast<object>().Any() ?? false);

        public static readonly IValueConverter IsNullOrEmpty =
            new FuncValueConverter<IEnumerable?, bool>(items => !(items?.Cast<object>().Any() ?? false));
    }
}
