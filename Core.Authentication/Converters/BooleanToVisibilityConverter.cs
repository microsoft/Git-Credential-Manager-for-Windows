using System;
using System.Globalization;
using System.Windows;

namespace Core.Converters
{
    [Localizability(LocalizationCategory.NeverLocalize)]
    public sealed class BooleanToVisibilityConverter : ValueConverterMarkupExtension<BooleanToVisibilityConverter>
    {
        private readonly System.Windows.Controls.BooleanToVisibilityConverter converter = new System.Windows.Controls.BooleanToVisibilityConverter();

        public override object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return converter.Convert(value, targetType, parameter, culture);
        }

        public override object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return converter.ConvertBack(value, targetType, parameter, culture);
        }
    }
}
