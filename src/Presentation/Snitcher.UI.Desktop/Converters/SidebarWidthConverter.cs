using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Snitcher.UI.Desktop.Converters
{
    public class SidebarWidthConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool boolValue ? (boolValue ? 240 : 64) : 240;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
