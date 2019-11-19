using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace IncinControl.Utilities
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
            if (parameter != null)
                _parameters = Regex.Split(parameter.ToString(), @"(?<!\\),");
        }

        private string GetParameter(IValueConverter converter)
        {
            if (_parameters == null)
                return null;

            var index = IndexOf(converter as IValueConverter);
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

            if (parameter != null)
                parameter = Regex.Unescape(parameter);

            return parameter;
        }
    }

    public class BoolToInvertedBoolConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Returns an inverted <c>Boolean</c>
        /// </summary>
        /// <param name="value"><c>Boolean</c></param>
        /// <returns>Returns a <c>Boolean</c></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool boolValue = (bool)value;
                return !boolValue;
            }
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack() of BoolToInvertedBoolConverter is not implemented");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

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
        {
            if (value == null)
                return FalseValue;
            else
                return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.Equals(TrueValue) : false;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class BoolToStringConverter : BoolToValueConverter<String> { }
    public class BoolToBrushConverter : BoolToValueConverter<Brush> { }
    public class BoolToObjectConverter : BoolToValueConverter<Object> { }

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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

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

            if (value == null || (string)value != (string)parameter)
                return defaultVisibility;
            else
                return matchedVisibility;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class DisplayNameAttributeConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value == null) return string.Empty;

            PropertyInfo propInfo = value.GetType().GetProperty(parameter.ToString());
            Object[] displayAttributes = propInfo.GetCustomAttributes(typeof(DisplayAttribute), true);
            if (displayAttributes != null && displayAttributes.Length == 1)
                return ((DisplayAttribute)displayAttributes[0]).Name;

            return propInfo.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}