using LYBT.Common.Enums.Users;

namespace LYBT.Module.Users.Dtos {

    /// <summary>
    /// 用户展示/返回 DTO，用于API查询结果
    /// </summary>
    public class UserDto {

        /// <summary>
        /// 用户唯一标识（主键，Guid 类型）
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
        /// 用户角色（管理员/医生等，枚举类型）
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// 用户可能拥有的多个角色列表
        /// </summary>
        public List<UserRole> Roles { get; set; } = new();

        /// <summary>
        /// 账号启用状态（true=启用，false=禁用）
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 最近登录时间
        /// </summary>
        public DateTime? LastLoginTime { get; set; }
    }
}