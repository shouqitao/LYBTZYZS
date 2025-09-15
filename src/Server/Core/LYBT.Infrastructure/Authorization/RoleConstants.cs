using System.ComponentModel;

namespace LYBT.Infrastructure.Authorization
{
    /// <summary>
    /// 角色常量定义 - 统一角色命名的唯一正源
    /// 目标：将 Doctor 作为主要角色，User 作为遗留兼容角色
    /// </summary>
    public static class RoleConstants
    {
        /// <summary>
        /// 管理员角色
        /// </summary>
        [Description("管理员")]
        public const string Admin = "Admin";

        /// <summary>
        /// 医生角色（主要角色）
        /// </summary>
        [Description("医生")]
        public const string Doctor = "Doctor";

        /// <summary>
        /// 遗留角色：用户（映射到医生角色）
        /// </summary>
        [Description("普通用户（兼容别名）")]
        [Obsolete("请使用 Doctor 角色。User 角色已统一为 Doctor 角色。", false)]
        public const string User = "User";

        /// <summary>
        /// 获取所有有效角色（排除遗留角色）
        /// </summary>
        public static readonly string[] ValidRoles = { Admin, Doctor };

        /// <summary>
        /// 获取所有角色（v1统一版本）
        /// </summary>
        public static readonly string[] AllRoles = { Admin, Doctor };

        /// <summary>
        /// 角色映射：标准化角色名称
        /// </summary>
        public static readonly Dictionary<string, string> RoleMapping = new()
        {
            [Doctor] = Doctor,   // Doctor -> Doctor 保持
            [Admin] = Admin // Admin -> Admin 保持
        };

        /// <summary>
        /// 角色中文显示名称映射 - v1简化版本
        /// </summary>
        public static readonly Dictionary<string, string> RoleDisplayNames = new()
        {
            [Admin] = "管理员",
            [Doctor] = "医生"
        };

        /// <summary>
        /// 标准化角色名称：将遗留角色名称转换为标准角色名称
        /// </summary>
        /// <param name="role">原始角色名称</param>
        /// <returns>标准化后的角色名称</returns>
        public static string NormalizeRole(string? role)
        {
            if (string.IsNullOrEmpty(role))
                return Doctor; // 默认角色

            // 处理大小写不敏感的映射
            var normalizedRole = role.Trim();

            // 直接映射查找
            if (RoleMapping.TryGetValue(normalizedRole, out var mappedRole))
                return mappedRole;

            // 中文角色映射
            return normalizedRole switch
            {
                "用户" or "普通用户" => Doctor,
                "医生" => Doctor,
                "管理员" => Admin,
                _ => Doctor // 默认映射到医生角色
            };
        }

        /// <summary>
        /// 获取角色的显示名称
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>角色显示名称</returns>
        public static string GetDisplayName(string? role)
        {
            if (string.IsNullOrEmpty(role))
                return RoleDisplayNames[Doctor];

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
            if (string.IsNullOrEmpty(role))
                return false;

            return AllRoles.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查角色是否为遗留角色 - v1版本无遗留角色
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>是否为遗留角色</returns>
        public static bool IsLegacyRole(string? role)
        {
            return false; // v1版本不存在遗留角色
        }
    }

    /// <summary>
    /// 角色策略常量
    /// </summary>
    public static class RolePolicies
    {
        /// <summary>
        /// 管理员策略
        /// </summary>
        public const string AdminPolicy = "AdminPolicy";

        /// <summary>
        /// 医生策略
        /// </summary>
        public const string DoctorPolicy = "DoctorPolicy";

        /// <summary>
        /// 医生或管理员策略
        /// </summary>
        public const string DoctorOrAdminPolicy = "DoctorOrAdminPolicy";

        /// <summary>
        /// 获取所有策略定义
        /// </summary>
        public static readonly Dictionary<string, string[]> PolicyRoles = new()
        {
            [AdminPolicy] = [RoleConstants.Admin],
            [DoctorPolicy] = [RoleConstants.Doctor],
            [DoctorOrAdminPolicy] = [RoleConstants.Doctor, RoleConstants.Admin]
        };
    }
}
