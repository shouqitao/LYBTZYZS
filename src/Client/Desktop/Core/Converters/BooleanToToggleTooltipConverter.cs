using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 布尔值转换为切换提示文本转换器
    /// true -> "禁用", false -> "启用"
    /// 显示点击后将执行的操作
    /// </summary>
    public class BooleanToToggleTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // true (已启用) -> 提示"禁用" (点击可禁用)
                // false (已禁用) -> 提示"启用" (点击可启用)
                return boolValue ? "禁用" : "启用";
            }

            return "切换状态";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToToggleTooltipConverter does not support ConvertBack");
        }
    }
}