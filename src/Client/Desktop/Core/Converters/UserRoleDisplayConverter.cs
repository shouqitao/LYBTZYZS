using LYBT.Shared.Models.Contracts.Common;
using System;
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
                "NURSE" => "护士",
                "PHARMACIST" => "药师",
                "RECEPTIONIST" => "前台",
                "CASHIER" => "收银员",
                "USER" => "普通用户",
                "GUEST" => "访客",
                "MANAGER" => "经理",
                "SUPERVISOR" => "主管",
                "STAFF" => "员工",
                _ => role // 如果没有匹配的，返回原始值
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string displayName || string.IsNullOrEmpty(displayName))
            {
                return "USER";
            }

            return displayName switch
            {
                "管理员" => "Admin",
                "医生" => "Doctor",
                "护士" => "Nurse", 
                "药师" => "Pharmacist",
                "前台" => "Receptionist",
                "收银员" => "Cashier",
                "普通用户" => "User",
                "访客" => "Guest",
                "经理" => "Manager",
                "主管" => "Supervisor",
                "员工" => "Staff",
                _ => displayName.ToUpper()
            };
        }
    }
}