using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 布尔值到字符串转换器
    /// OpenSpec: unify-medicalcase-view-edit-pattern
    /// </summary>
    public class BoolToStringConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// 当值为 true 时显示的文本
        /// </summary>
        public string TrueValue { get; set; } = "是";

        /// <summary>
        /// 当值为 false 时显示的文本
        /// </summary>
        public string FalseValue { get; set; } = "否";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue == TrueValue;
            }
            return false;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
