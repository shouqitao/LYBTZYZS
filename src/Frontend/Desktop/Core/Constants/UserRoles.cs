using System;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.WPF.Client.Core.Constants
{
    /// <summary>
    /// 用户角色常量 - 已弃用，请直接使用LYBT.Shared.Models.Enums.UserRole
    /// </summary>
    [Obsolete("请直接使用LYBT.Shared.Models.Enums.UserRole和相应的扩展方法")]
    public static class UserRoles
    {
        // 保留为向后兼容，但建议使用UserRole枚举
        public const string Admin = "Admin";
        public const string Doctor = "DiagnosingDoctor";
        public const string Receptionist = "RegistrationStaff";
        public const string Cashier = "CashierStaff";
        public const string Pharmacist = "PharmacyStaff";
        
        /// <summary>
        /// 获取角色显示名称 - 建议直接使用UserRole.GetDisplayName()
        /// </summary>
        public static string GetDisplayName(UserRole role) => role.GetDisplayName();
    }
}