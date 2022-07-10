using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosey.Gui.Views.Converters
{
    public class DisplayNameAttributeConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var propInfo = value.GetType().GetProperty(parameter.ToString());
            var displayAttributes = propInfo.GetCustomAttributes(typeof(DisplayAttribute), true);

            return displayAttributes is not null && displayAttributes.Length == 1
                ? ((DisplayAttribute)displayAttributes[0]).Name
                : propInfo.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
