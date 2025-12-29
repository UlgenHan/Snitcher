using Avalonia.Data.Converters;
using Avalonia.Layout;
using System;
using System.Globalization;

namespace Snitcher.UI.Desktop.Converters
{
    public class IconAlignmentConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool boolValue ? (boolValue ? HorizontalAlignment.Left : HorizontalAlignment.Center) : HorizontalAlignment.Left;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
