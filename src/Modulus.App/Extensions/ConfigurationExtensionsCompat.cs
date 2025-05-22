using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensionsCompat
    {
        public static T GetValue<T>(this IConfiguration configuration, string key, T defaultValue = default!)
        {
            var value = configuration[key];
            if (value == null)
                return defaultValue;
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
