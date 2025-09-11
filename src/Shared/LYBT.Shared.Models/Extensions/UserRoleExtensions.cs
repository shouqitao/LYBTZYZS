using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{

    /// <summary>
    /// 用户角色扩展方法 - Record-Only模式兼容性映射
    /// </summary>
    public static class UserRoleExtensions
    {

        /// <summary>
        /// 将旧角色映射到新的简化角色（Admin/User）
        /// </summary>
        /// <param name="role">原始角色</param>
        /// <returns>映射后的简化角色</returns>
        public static UserRole ToSimplifiedRole(this UserRole role)
        {
            return role switch
            {
                // 管理员角色映射
                UserRole.Admin => UserRole.Admin,

                // 普通用户角色映射
#pragma warning disable CS0618 // Type or member is obsolete
                UserRole.Doctor => UserRole.User,
                UserRole.Pharmacist => UserRole.User,
                UserRole.Receptionist => UserRole.User,
                UserRole.Cashier => UserRole.User,
                UserRole.Therapist => UserRole.User,
#pragma warning restore CS0618 // Type or member is obsolete

                // 已经是简化角色的直接返回
                UserRole.User => UserRole.User,

                // 默认映射到普通用户
                _ => UserRole.User
            };
        }

        /// <summary>
        /// 检查角色是否为管理员（包含兼容映射）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否为管理员</returns>
        public static bool IsAdmin(this UserRole role)
        {
            return role.ToSimplifiedRole() == UserRole.Admin;
        }

        /// <summary>
        /// 检查角色是否为普通用户（包含兼容映射）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否为普通用户</returns>
        public static bool IsUser(this UserRole role)
        {
            return role.ToSimplifiedRole() == UserRole.User;
        }

        /// <summary>
        /// 获取角色的显示名称（兼容旧角色）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(this UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "管理员",
                UserRole.User => "普通用户",
#pragma warning disable CS0618 // Type or member is obsolete
                UserRole.Doctor => "医生（普通用户）",
                UserRole.Pharmacist => "药师（普通用户）",
                UserRole.Receptionist => "前台（普通用户）",
                UserRole.Cashier => "收银员（普通用户）",
                UserRole.Therapist => "理疗师（普通用户）",
#pragma warning restore CS0618 // Type or member is obsolete
                _ => "未知角色"
            };
        }

        /// <summary>
        /// 获取角色对应的 Policy 名称（用于授权检查）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>Policy 名称</returns>
        public static string GetPolicyName(this UserRole role)
        {
            return role.ToSimplifiedRole() switch
            {
                UserRole.Admin => "AdminPolicy",
                UserRole.User => "UserPolicy",
                _ => "UserPolicy" // 默认为用户权限
            };
        }

        /// <summary>
        /// 从字符串解析角色（支持兼容映射）
        /// </summary>
        /// <param name="roleString">角色字符串</param>
        /// <returns>解析后的角色</returns>
        public static UserRole ParseRole(string roleString)
        {
            if (string.IsNullOrWhiteSpace(roleString))
                return UserRole.User;

            return roleString.ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin,
                "user" => UserRole.User,

                // 兼容旧角色名称
                "doctor" => UserRole.User,       // 映射到 User
                "pharmacist" => UserRole.User,   // 映射到 User
                "receptionist" => UserRole.User, // 映射到 User
                "cashier" => UserRole.User,      // 映射到 User
                "therapist" => UserRole.User,    // 映射到 User

                _ => UserRole.User
            };
        }

        /// <summary>
        /// 检查角色是否有特定权限（Record-Only权限模型）
        /// </summary>
        /// <param name="role">角色</param>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        public static bool HasPermission(this UserRole role, string permission)
        {
            var simplifiedRole = role.ToSimplifiedRole();

            return permission.ToLowerInvariant() switch
            {
                // 管理员专有权限
                "user.manage" => simplifiedRole == UserRole.Admin,
                "system.config" => simplifiedRole == UserRole.Admin,
                "backup.restore" => simplifiedRole == UserRole.Admin,

                // 用户通用权限（Record-Only基础功能）
                "patient.read" => true,
                "patient.write" => true,
                "medicalcase.read" => true,
                "medicalcase.write" => true,
                "consultation.read" => true,
                "consultation.write" => true,
                "prescription.read" => true,
                "prescription.write" => true,
                "herb.read" => true,
                "formula.read" => true,

                // 默认拒绝未知权限
                _ => false
            };
        }
    }
}
