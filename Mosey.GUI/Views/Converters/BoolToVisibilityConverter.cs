using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mosey.GUI.Views.Converters
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

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueValue))
                return true;
            if (Equals(value, FalseValue))
                return false;
            return null;
        }
    }
}
