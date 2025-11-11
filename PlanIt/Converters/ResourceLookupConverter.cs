using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PlanIt.Converters;

public class ResourceLookupConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            var theme = Application.Current?.RequestedThemeVariant;
            var styles = Application.Current?.Styles;
            
            foreach (var style in styles)
            {
                if (style.TryGetResource(key, theme, out var resource))
                {
                    switch (resource)
                    {
                        case Color color:
                            return new SolidColorBrush(color);
                        case IBrush brush:
                            return brush;
                        case StreamGeometry svg:
                            return svg;
                    }
                }
            }
        }
        
        Console.WriteLine($"[ResourceConverter] Resource not found for key: {value}\n");

        return AvaloniaProperty.UnsetValue;

    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
