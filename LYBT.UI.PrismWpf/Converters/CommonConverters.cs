using System.Globalization;
using System.Windows.Data;

namespace LYBT.UI.PrismWpf.Converters
{
    /// <summary>
    /// 布尔值转换为启用/禁用文本的转换器
    /// </summary>
    public class BooleanToActiveTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "禁用" : "启用";
            }
            return "启用";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值转换为状态文本的转换器
    /// </summary>
    public class BooleanToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "启用" : "禁用";
            }
            return "禁用";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 空值转换为默认文本的转换器
    /// </summary>
    public class NullToDefaultTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return parameter?.ToString() ?? "-";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 角色枚举转换为中文文本的转换器
    /// </summary>
    public class RoleToChineseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                return role switch
                {
                    "Admin" => "管理员",
                    "Doctor" => "医生",
                    "Receptionist" => "前台",
                    "Cashier" => "收银员",
                    "Pharmacist" => "药师",
                    _ => role
                };
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}