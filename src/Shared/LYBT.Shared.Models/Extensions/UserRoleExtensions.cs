using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{

    /// <summary>
    /// 用户角色扩展方法 - 角色统一为 Doctor 模式兼容性映射
    /// </summary>
    public static class UserRoleExtensions
    {

        /// <summary>
        /// 将旧角色映射到统一角色（Admin/Doctor）
        /// </summary>
        /// <param name="role">原始角色</param>
        /// <returns>映射后的统一角色</returns>
        public static UserRole ToUnifiedRole(this UserRole role)
        {
            return role switch
            {
                // 管理员角色映射
                UserRole.Admin => UserRole.Admin,

                // 医生角色（主要角色）
                UserRole.Doctor => UserRole.Doctor,

                // 遗留角色映射到医生角色
#pragma warning disable CS0618 // Type or member is obsolete
                UserRole.User => UserRole.Doctor,
                UserRole.Pharmacist => UserRole.Doctor,
                UserRole.Receptionist => UserRole.Doctor,
                UserRole.Cashier => UserRole.Doctor,
                UserRole.Therapist => UserRole.Doctor,
#pragma warning restore CS0618 // Type or member is obsolete

                // 默认映射到医生角色
                _ => UserRole.Doctor
            };
        }

        /// <summary>
        /// 检查角色是否为管理员（包含兼容映射）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否为管理员</returns>
        public static bool IsAdmin(this UserRole role)
        {
            return role.ToUnifiedRole() == UserRole.Admin;
        }

        /// <summary>
        /// 检查角色是否为医生（包含兼容映射）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否为医生</returns>
        public static bool IsDoctor(this UserRole role)
        {
            return role.ToUnifiedRole() == UserRole.Doctor;
        }

        /// <summary>
        /// 检查角色是否为普通用户（兼容性方法，映射到医生角色）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否为普通用户（实际为医生）</returns>
        [Obsolete("使用 IsDoctor() 方法。User 角色已统一为 Doctor 角色。", false)]
        public static bool IsUser(this UserRole role)
        {
            return role.ToUnifiedRole() == UserRole.Doctor;
        }

        /// <summary>
        /// 获取角色的显示名称（统一为医生角色）
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(this UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "管理员",
                UserRole.Doctor => "医生",
#pragma warning disable CS0618 // Type or member is obsolete
                UserRole.User => "普通用户（已统一为医生）",
                UserRole.Pharmacist => "药师（已统一为医生）",
                UserRole.Receptionist => "前台（已统一为医生）",
                UserRole.Cashier => "收银员（已统一为医生）",
                UserRole.Therapist => "理疗师（已统一为医生）",
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
            return role.ToUnifiedRole() switch
            {
                UserRole.Admin => "AdminPolicy",
                UserRole.Doctor => "DoctorPolicy",
                _ => "DoctorPolicy" // 默认为医生权限
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
                return UserRole.Doctor;

            return roleString.ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin,
                "doctor" => UserRole.Doctor,

                // 兼容旧角色名称，统一映射到 Doctor
                "user" => UserRole.Doctor,       // 映射到 Doctor
                "pharmacist" => UserRole.Doctor,   // 映射到 Doctor
                "receptionist" => UserRole.Doctor, // 映射到 Doctor
                "cashier" => UserRole.Doctor,      // 映射到 Doctor
                "therapist" => UserRole.Doctor,    // 映射到 Doctor

                _ => UserRole.Doctor
            };
        }

        /// <summary>
        /// 检查角色是否有特定权限（Record-Only权限模型，统一为Doctor角色）
        /// </summary>
        /// <param name="role">角色</param>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        public static bool HasPermission(this UserRole role, string permission)
        {
            var unifiedRole = role.ToUnifiedRole();

            return permission.ToLowerInvariant() switch
            {
                // 管理员专有权限
                "user.manage" => unifiedRole == UserRole.Admin,
                "system.config" => unifiedRole == UserRole.Admin,
                "backup.restore" => unifiedRole == UserRole.Admin,

                // 医生通用权限（Record-Only基础功能）
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
