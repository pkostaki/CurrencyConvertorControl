using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace CustomControlLibrary
{
    /// <summary>
    /// Converts string in and from of double type.
    /// </summary>
    public class NumberToStringConvertor : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = (double)value;
            return result.ToString(CultureInfo.CurrentUICulture.NumberFormat);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var processText = ProcessTextToNumberRepresentation(value as string);

            if (!double.TryParse(processText, NumberStyles.Number, CultureInfo.CurrentUICulture, out double numberResult)
                || processText.IndexOf(CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator) == processText.Length - 1)
            {
                return Binding.DoNothing;
            }

            return numberResult;
        }

        private static string ProcessTextToNumberRepresentation(string input)
        {
            try
            {
                var ds = CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator;
                var regexpr = string.Join("", @"(\d*(\", ds, @"\d*){0,1}){1}");
                var re = new Regex(regexpr, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var match = re.Match(input);
                if (match.Success)
                {
                    return match.Value;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Nothing to do
            }
            return string.Empty;
        }
    }
}