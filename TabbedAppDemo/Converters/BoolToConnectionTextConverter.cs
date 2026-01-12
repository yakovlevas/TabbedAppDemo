using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace TabbedAppDemo.Converters
{
    public class BoolToConnectionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? "Подключено" : "Отключено";
            }
            return "Отключено";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}