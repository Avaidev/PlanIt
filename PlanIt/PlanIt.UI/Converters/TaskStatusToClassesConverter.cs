using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PlanIt.UI.Converters;

public class TaskStatusToClassesConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
       if (values is not [bool isImportant, bool isDone, DateTime completeDate]) return new List<string>{"task"};
       var classes = new List<string>{"task"};
       
       if (isImportant) classes.Add("important");
       if (isDone) classes.Add("completed");
       else if (completeDate < DateTime.Now) classes.Add("missed");
       
       return classes;
    }
}