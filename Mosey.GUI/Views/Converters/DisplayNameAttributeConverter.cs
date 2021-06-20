using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosey.GUI.Views.Converters
{
    public class DisplayNameAttributeConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value is null) return string.Empty;

            var propInfo = value.GetType().GetProperty(parameter.ToString());
            var displayAttributes = propInfo.GetCustomAttributes(typeof(DisplayAttribute), true);
            if (displayAttributes is not null && displayAttributes.Length == 1)
            {
                return ((DisplayAttribute)displayAttributes[0]).Name;
            }

            return propInfo.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
