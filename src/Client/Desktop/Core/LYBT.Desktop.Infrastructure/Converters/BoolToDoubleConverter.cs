using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 布尔值到双精度浮点数转换器
    /// OpenSpec: refactor-role-navigation
    /// 用于侧边栏宽度等场景
    /// </summary>
    public class BoolToDoubleConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// 当值为 true 时的数值
        /// </summary>
        public double TrueValue { get; set; } = 200;

        /// <summary>
        /// 当值为 false 时的数值
        /// </summary>
        public double FalseValue { get; set; } = 56;

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
            if (value is double doubleValue)
            {
                return Math.Abs(doubleValue - TrueValue) < 0.001;
            }
            return false;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
