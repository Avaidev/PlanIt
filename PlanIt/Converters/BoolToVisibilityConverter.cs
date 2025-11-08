using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ExCSS;

namespace PlanIt.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        value != null && (bool)value ? Visibility.Visible : Visibility.Collapse;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value != null && (Visibility)value == Visibility.Visible;
}