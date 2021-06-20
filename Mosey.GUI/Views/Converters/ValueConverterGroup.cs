using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Mosey.GUI.Views.Converters
{
    public class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        private string[] _parameters;
        private bool _shouldReverse = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExtractParameters(parameter);

            if (_shouldReverse)
            {
                Reverse();
                _shouldReverse = false;
            }

            return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, GetParameter(converter), culture));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ExtractParameters(parameter);

            Reverse();
            _shouldReverse = true;

            return this.Aggregate(value, (current, converter) => converter.ConvertBack(current, targetType, GetParameter(converter), culture));
        }

        private void ExtractParameters(object parameter)
        {
            if (parameter is not null)
            {
                _parameters = Regex.Split(parameter.ToString(), @"(?<!\\),");
            }
        }

        private string GetParameter(IValueConverter converter)
        {
            if (_parameters is null)
            {
                return null;
            }

            var index = IndexOf(converter);
            string parameter;

            try
            {
                parameter = _parameters[index];
            }

            catch (IndexOutOfRangeException ex)
            {
                System.Console.WriteLine(ex.Message);
                parameter = null;
            }

            if (parameter is not null)
            {
                parameter = Regex.Unescape(parameter);
            }

            return parameter;
        }
    }
}
