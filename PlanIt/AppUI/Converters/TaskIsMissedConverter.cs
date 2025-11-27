using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PlanIt.UI.Converters;

public class TaskIsMissedConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [DateTime completeDate, bool isDone]) return false;
        if (!isDone && completeDate < DateTime.Now) return true;
        return false;
    }
}