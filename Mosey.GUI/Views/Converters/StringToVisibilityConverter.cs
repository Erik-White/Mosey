using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosey.GUI.Views.Converters
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
            Visibility matchedVisibility = (Visibility)Enum.Parse(typeof(Visibility), MatchedVisibility, true);
            Visibility defaultVisibility = (Visibility)Enum.Parse(typeof(Visibility), DefaultVisibility, true);

            if (value == null || (string)value != (string)parameter)
            {
                return defaultVisibility;
            }
            else
            {
                return matchedVisibility;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
