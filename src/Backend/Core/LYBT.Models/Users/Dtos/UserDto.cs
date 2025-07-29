using LYBT.Common.Enums.Users;
using LYBT.Common.Extensions;
using System.ComponentModel;

namespace LYBT.Models.Users {

    /// <summary>
    /// 用户展示/返回 DTO，用于API查询结果
    /// </summary>
    public class UserDto {

        /// <summary>
        /// 用户唯一标识（主键，Guid 类型）
        /// </summary>
        [DisplayName("用户唯一标识（主键，Guid 类型）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 用户名（唯一）
        /// </summary>
        [DisplayName("用户名（唯一）")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名
        /// </summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 用户拥有的所有角色列表
        /// </summary>
        [DisplayName("用户角色列表")]
        public List<UserRole> Roles { get; set; } = new();

        /// <summary>
        /// 主要角色（用于向后兼容）
        /// </summary>
        public UserRole PrimaryRole => Roles.FirstOrDefault();

        /// <summary>
        /// 用户角色文本，显示所有角色名称
        /// </summary>
        public string RolesText => Roles.Count > 0
            ? string.Join("、", Roles.Select(r => r.GetDescription()))
            : string.Empty;

        /// <summary>
        /// 账号启用状态（true=启用，false=禁用）
        /// </summary>
        [DisplayName("账号启用状态（true=启用，false=禁用）")]
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 最近登录时间
        /// </summary>
        [DisplayName("最近登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// 邮箱地址
        /// </summary>
        [DisplayName("邮箱地址")]
        public string? Email { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [DisplayName("联系电话")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        [DisplayName("用户角色")]
        public UserRole Role { get; set; }

        /// <summary>
        /// 是否有管理员权限
        /// </summary>
        public bool IsAdmin => Role == UserRole.Admin;

        /// <summary>
        /// 是否有医生权限
        /// </summary>
        public bool IsDoctor => Role == UserRole.DiagnosingDoctor;
    }
}