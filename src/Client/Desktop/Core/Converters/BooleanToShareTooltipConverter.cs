using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 布尔值转换为共享提示文本转换器
    /// true -> "取消共享", false -> "设为共享"
    /// 显示点击后将执行的操作
    /// </summary>
    public class BooleanToShareTooltipConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // true (已共享) -> 提示"取消共享" (点击可取消共享)
                // false (未共享) -> 提示"设为共享" (点击可设为共享)
                return boolValue ? "取消共享" : "设为共享";
            }

            return "切换共享状态";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToShareTooltipConverter does not support ConvertBack");
        }
    }
}
