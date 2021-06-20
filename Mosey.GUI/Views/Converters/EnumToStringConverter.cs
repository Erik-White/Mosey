using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosey.GUI.Views.Converters
{
    public class EnumToStringConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Return a <c>String</c> from an Enum
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string EnumString;
            try
            {
                EnumString = Enum.GetName((value.GetType()), value);
                return EnumString;
            }
            catch
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
