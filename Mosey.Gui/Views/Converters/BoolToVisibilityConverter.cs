using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mosey.Gui.Views.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Returns a <c>Visibility</c> from a <c>Boolean</c> value
        /// Allows the default <c>Visibility to be specified</c>
        /// </summary>
        /// <param name="value"><c>Boolean</c></param>
        /// <returns>Returns a <c>Visibility</c></returns>
        public Visibility TrueValue { get; set; }
        public Visibility FalseValue { get; set; }

        public BoolToVisibilityConverter()
        {
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not bool booleanValue
                ? null
                : booleanValue ? TrueValue : FalseValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Equals(value, TrueValue)
                ? true
                : Equals(value, FalseValue) ? false : null;
    }
}
