using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosey.UI.Views.Converters
{
    public class BoolToValueConverter<T> : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Convert a <c>Boolean</c> <c>True</c>/<c>False</c> to the desired output
        /// </summary>
        /// <param name="TrueValue">The value to return for <c>True</c></param>
        /// <param name="FalseValue">The value to return for <c>False</c></param>
        public T TrueValue { get; set; }
        public T FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not null && (bool)value
                ? TrueValue
                : FalseValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not null && value.Equals(TrueValue);

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }

    public class BoolToStringConverter : BoolToValueConverter<string> { }
    public class BoolToBrushConverter : BoolToValueConverter<Brush> { }
    public class BoolToObjectConverter : BoolToValueConverter<object> { }
}
