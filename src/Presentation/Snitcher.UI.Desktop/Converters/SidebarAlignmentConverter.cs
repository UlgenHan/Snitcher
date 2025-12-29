using Avalonia.Data.Converters;
using Avalonia.Layout;
using System;
using System.Globalization;

namespace Snitcher.UI.Desktop.Converters
{
    public class SidebarAlignmentConverter : IValueConverter
    {
        public object? Convert(object? value, Type t, object? p, CultureInfo c)
            => HorizontalAlignment.Left;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
