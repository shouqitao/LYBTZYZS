using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// 锁定状态转换器
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class LockoutStatusConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not DateTime && value is not null)
                return "正常";

            var lockoutEnd = value as DateTime?;
            if (!lockoutEnd.HasValue)
                return "正常";

            return lockoutEnd.Value > DateTime.Now ? "已锁定" : "正常";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException("LockoutStatusConverter does not support ConvertBack");
        }
    }
}
