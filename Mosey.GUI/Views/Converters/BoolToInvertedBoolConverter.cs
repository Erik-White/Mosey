using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosey.GUI.Views.Converters
{
    public class BoolToInvertedBoolConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Returns an inverted <c>Boolean</c>
        /// </summary>
        /// <param name="value"><c>Boolean</c></param>
        /// <returns>Returns a <c>Boolean</c></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == DependencyProperty.UnsetValue
                ? null
                : value is bool boolValue && !boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException("ConvertBack() of BoolToInvertedBoolConverter is not implemented");

        public override object ProvideValue(IServiceProvider serviceProvider)
            => this;
    }
}
