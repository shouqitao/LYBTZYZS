using System;
using System.Globalization;
using System.Windows.Data;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 身份证号码掩码转换器
    /// </summary>
    public class IDNumberMaskConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string idNumber && !string.IsNullOrWhiteSpace(idNumber))
            {
                if (idNumber.Length >= 18)
                {
                    // 显示前6位和后4位，中间用*替代
                    return $"{idNumber.Substring(0, 6)}********{idNumber.Substring(14)}";
                }
                else if (idNumber.Length > 10)
                {
                    // 对于非标准长度，显示前3位和后3位
                    return $"{idNumber.Substring(0, 3)}***{idNumber.Substring(idNumber.Length - 3)}";
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
