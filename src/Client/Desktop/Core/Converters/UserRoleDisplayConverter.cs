using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 用户角色显示转换器
    /// 将角色代码转换为中文显示
    /// </summary>
    public class UserRoleDisplayConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string role || string.IsNullOrEmpty(role))
            {
                return "未知角色";
            }

            return role.ToUpper() switch
            {
                "ADMIN" => "管理员",
                "ADMINISTRATOR" => "管理员",
                "DOCTOR" => "医生",
                "PHARMACIST" => "药师",
                "RECEPTIONIST" => "前台",
                "CASHIER" => "收银员",
                "THERAPIST" => "理疗师",
                _ => role // 如果没有匹配的，返回原始值
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string displayName || string.IsNullOrEmpty(displayName))
            {
                return "Doctor";
            }

            return displayName switch
            {
                "管理员" => "Admin",
                "医生" => "Doctor",
                "药师" => "Pharmacist",
                "前台" => "Receptionist",
                "收银员" => "Cashier",
                "理疗师" => "Therapist",
                _ => displayName.ToUpper()
            };
        }
    }
}
