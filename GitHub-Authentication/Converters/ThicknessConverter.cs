using System;
using System.Windows;
using System.Windows.Data;

namespace GitHub.UI
{
    public class ThicknessConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            var t = ((Thickness)value);

            return (t.Left + t.Right + t.Top + t.Bottom) / 4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}