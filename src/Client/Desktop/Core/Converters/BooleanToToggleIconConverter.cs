using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 布尔值转换为切换图标转换器
    /// true -> 暂停图标 (禁用), false -> 播放图标 (启用)
    /// 使用 Segoe MDL2 Assets 字体图标
    /// </summary>
    public class BooleanToToggleIconConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // true (已启用) -> 显示暂停图标 (点击可禁用)
                // false (已禁用) -> 显示播放图标 (点击可启用)
                return boolValue ? "\uE769" : "\uE768"; // Pause : Play
            }

            return "\uE768"; // 默认播放图标
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToToggleIconConverter does not support ConvertBack");
        }
    }
}
