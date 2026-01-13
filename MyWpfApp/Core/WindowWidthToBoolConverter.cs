using System;
using System.Globalization;
using System.Windows.Data;

namespace AdminClientWpf.Core
{
    public class WindowWidthToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && double.TryParse(parameter?.ToString(), out var threshold))
            {
                return width < threshold;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
