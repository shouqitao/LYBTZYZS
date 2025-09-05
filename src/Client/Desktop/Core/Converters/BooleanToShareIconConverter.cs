using System;
using System.Globalization;
using System.Windows.Data;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 布尔值转换为共享图标转换器
    /// true -> 取消共享图标, false -> 共享图标
    /// 使用 Segoe MDL2 Assets 字体图标
    /// </summary>
    public class BooleanToShareIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // true (已共享) -> 显示取消共享图标 (点击可取消共享)
                // false (未共享) -> 显示共享图标 (点击可设为共享)
                return boolValue ? "\uE72D" : "\uE72E"; // ShareStop : Share
            }

            return "\uE72E"; // 默认共享图标
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToShareIconConverter does not support ConvertBack");
        }
    }
}
