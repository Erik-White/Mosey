using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosey.Gui.Views.Converters
{
    public class StringToVisibilityConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Return a <c>Visibility</c> if an input <c>String</c> is matched
        /// </summary>
        /// <param name="DefaultVisibility">The <c>Visibility</c> type to return if string does not match</param>
        /// <param name="parameter">The <c>String</c> to match</param>
        public string MatchedVisibility { get; set; }
        public string DefaultVisibility { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var matchedVisibility = (Visibility)Enum.Parse(typeof(Visibility), MatchedVisibility, true);
            var defaultVisibility = (Visibility)Enum.Parse(typeof(Visibility), DefaultVisibility, true);

            return value is null || (string)value != (string)parameter
                ? defaultVisibility
                : matchedVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
