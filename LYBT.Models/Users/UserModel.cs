using System;
using LYBT.Common.Enums;

namespace LYBT.Module.Users.Models {
    /// <summary>
    /// 用户实体类，数据库映射
    /// </summary>
    public class UserModel {
        /// <summary>
        /// 用户唯一标识（主键）
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 用户名（唯一）
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色（管理员、医生等，枚举）
        /// </summary>
        public UserRole Role { get; set; } = UserRole.DiagnosingDoctor;

        /// <summary>
        /// 启用状态（true=启用，false=禁用）
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 最近登录时间
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// 密码哈希（可选，如有安全需求可加盐等字段）
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        // 如有邮箱、手机号等请补充对应字段
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
