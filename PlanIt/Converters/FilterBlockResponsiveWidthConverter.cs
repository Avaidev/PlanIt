using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PlanIt.Converters
{
    public class FilterBlockResponsiveWidthConverter : IMultiValueConverter
    {
        private const int DIGIT_PIXELS = 15;
        private const int DEFAULT_FILTER_BTN_LEN = 50;
        
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [double panelWidth, string textContent]) return values[0];
            int digitCount = textContent.Count(char.IsDigit);
            
            return CalculateResponsiveWidth(panelWidth, digitCount);

        }
        
        private double CalculateResponsiveWidth(double panelWidth, int digitCount)
        {
            var half = panelWidth / 2 - 6;
            var len = DEFAULT_FILTER_BTN_LEN + DIGIT_PIXELS * digitCount;
            if (len < 105) len = 105;
            return half < len
                ? panelWidth
                : half;
        }
    }
}