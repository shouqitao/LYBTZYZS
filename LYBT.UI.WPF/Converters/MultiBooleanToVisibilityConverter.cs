using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// 多值布尔到可见性转换器
    /// </summary>
    public class MultiBooleanToVisibilityConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            bool result = true;
            string operation = parameter?.ToString()?.ToUpper() ?? "AND";

            foreach (var value in values) {
                if (value is bool boolValue) {
                    if (operation == "OR") {
                        if (boolValue) {
                            result = true;
                            break;
                        }
                        result = false;
                    } else // AND
                      {
                        result = result && boolValue;
                        if (!result)
                            break;
                    }
                }
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
