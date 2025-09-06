using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters {

    /// <summary>
    /// 获取字符串第一个字符的转换器
    /// </summary>
    public class FirstCharacterConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string str && !string.IsNullOrWhiteSpace(str)) {
                return str.Substring(0, 1).ToUpper();
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
