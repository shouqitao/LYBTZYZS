namespace LYBT.Shared.Utilities.Security
{
    /// <summary>
    /// 角色管理帮助类
    /// </summary>
    public static class RoleHelper
    {
        /// <summary>
        /// 系统角色常量
        /// </summary>
        public static class Roles
        {
            /// <summary>
            /// 管理员角色
            /// </summary>
            public const string Admin = "Admin";

            /// <summary>
            /// 医生角色
            /// </summary>
            public const string Doctor = "Doctor";

            /// <summary>
            /// 获取所有有效角色
            /// </summary>
            public static readonly string[] All = { Admin, Doctor };
        }

        /// <summary>
        /// 角色策略名称
        /// </summary>
        public static class Policies
        {
            /// <summary>
            /// 管理员策略
            /// </summary>
            public const string AdminOnly = "AdminPolicy";

            /// <summary>
            /// 医生策略
            /// </summary>
            public const string DoctorOnly = "DoctorPolicy";

            /// <summary>
            /// 医生或管理员策略
            /// </summary>
            public const string DoctorOrAdmin = "DoctorOrAdminPolicy";
        }

        /// <summary>
        /// 角色显示名称映射
        /// </summary>
        private static readonly Dictionary<string, string> RoleDisplayNames = new()
        {
            [Roles.Admin] = "管理员",
            [Roles.Doctor] = "医生"
        };

        /// <summary>
        /// 标准化角色名称
        /// </summary>
        /// <param name="role">原始角色名称</param>
        /// <returns>标准化后的角色名称</returns>
        public static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return Roles.Doctor; // 默认角色

            var normalizedRole = role.Trim();

            // 中文角色映射
            return normalizedRole switch
            {
                "用户" or "普通用户" or "User" => Roles.Doctor,
                "医生" or "Doctor" => Roles.Doctor,
                "管理员" or "Admin" => Roles.Admin,
                _ => Roles.Doctor // 默认映射到医生角色
            };
        }

        /// <summary>
        /// 获取角色的显示名称
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>角色显示名称</returns>
        public static string GetDisplayName(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return RoleDisplayNames[Roles.Doctor];

            var normalizedRole = NormalizeRole(role);
            return RoleDisplayNames.TryGetValue(normalizedRole, out var displayName)
                ? displayName
                : normalizedRole;
        }

        /// <summary>
        /// 检查角色是否有效
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>是否为有效角色</returns>
        public static bool IsValidRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            var normalizedRole = NormalizeRole(role);
            return Roles.All.Contains(normalizedRole, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查是否为管理员角色
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>是否为管理员</returns>
        public static bool IsAdmin(string? role)
        {
            return string.Equals(NormalizeRole(role), Roles.Admin, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查是否为医生角色
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>是否为医生</returns>
        public static bool IsDoctor(string? role)
        {
            return string.Equals(NormalizeRole(role), Roles.Doctor, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取策略对应的角色列表
        /// </summary>
        /// <param name="policyName">策略名称</param>
        /// <returns>角色列表</returns>
        public static string[] GetPolicyRoles(string policyName)
        {
            return policyName switch
            {
                Policies.AdminOnly => new[] { Roles.Admin },
                Policies.DoctorOnly => new[] { Roles.Doctor },
                Policies.DoctorOrAdmin => new[] { Roles.Doctor, Roles.Admin },
                _ => Array.Empty<string>()
            };
        }
    }
}
