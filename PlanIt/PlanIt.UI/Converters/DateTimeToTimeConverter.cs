using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PlanIt.UI.Converters;

public class DateTimeToTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.TimeOfDay.ToString("hh\\:mm");
        }

        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}