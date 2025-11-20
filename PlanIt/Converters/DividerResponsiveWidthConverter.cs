using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PlanIt.Converters;

public class DividerResponsiveWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double dividerWidth)
        {
           return dividerWidth - 12;
        }
        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}