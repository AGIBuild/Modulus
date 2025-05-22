using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Modulus.App.Converters;

/// <summary>
/// 将导航项目的活动状态转换为相应的背景色
/// </summary>
public class NavItemBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            // 活动菜单项使用渐变色
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative)
            };
            
            gradientBrush.GradientStops.Add(new GradientStop(Color.Parse("#3B82F6"), 0.0)); // 主蓝色
            gradientBrush.GradientStops.Add(new GradientStop(Color.Parse("#2563EB"), 1.0)); // 深蓝色
            
            return gradientBrush;
        }
        
        // 非活动菜单项使用半透明背景
        return new SolidColorBrush(Color.Parse("#22FFFFFF"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
} 