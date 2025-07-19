using LYBT.Common.Extensions;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// Ã¶¾ÙÃèÊö×ª»»Æ÷
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(string))]
    public class EnumDescriptionConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not Enum enumValue)
                return value?.ToString() ?? string.Empty;

            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field == null)
                return enumValue.ToString();

            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute?.Description ?? enumValue.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException("EnumDescriptionConverter does not support ConvertBack");
        }
    }
}
