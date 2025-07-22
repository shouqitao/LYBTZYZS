using LYBT.Common.Enums.Users;
using LYBT.Common.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace LYBT.Module.Users.Dtos {

    /// <summary>
    /// 用户展示/返回 DTO，用于API查询结果
    /// </summary>
    public class UserDto {

        /// <summary>
        /// 用户唯一标识（主键，Guid 类型）
        /// </summary>
        [DisplayName("用户唯一标识（主键，Guid 类型）")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 用户名（唯一）
        /// </summary>
        [DisplayName("用户名（唯一）")]
/// <summary>
/// UserName 属性。
/// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名
        /// </summary>
        [DisplayName("真实姓名")]
/// <summary>
/// RealName 属性。
/// </summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色（管理员/医生等，枚举类型）
        /// </summary>
        [DisplayName("用户角色（管理员/医生等，枚举类型）")]
/// <summary>
/// Role 属性。
/// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// 用户可能拥有的多个角色列表
        /// </summary>
        [DisplayName("用户可能拥有的多个角色列表")]
/// <summary>
/// Roles 属性。
/// </summary>
        public List<UserRole> Roles { get; set; } = new();

        /// <summary>
        /// 用户角色文本，显示所有角色名称
        /// </summary>
        public string RolesText =>
            Roles != null && Roles.Count > 0 ?
                string.Join("、", Roles.Select(r => r.GetDescription())) :
                Role.GetDescription();

        /// <summary>
        /// 账号启用状态（true=启用，false=禁用）
        /// </summary>
        [DisplayName("账号启用状态（true=启用，false=禁用）")]
/// <summary>
/// IsActive 属性。
/// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
/// <summary>
/// CreatedTime 属性。
/// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 最近登录时间
        /// </summary>
        [DisplayName("最近登录时间")]
/// <summary>
/// LastLoginTime 属性。
/// </summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// 邮箱地址
        /// </summary>
        [DisplayName("邮箱地址")]
/// <summary>
/// Email 属性。
/// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [DisplayName("联系电话")]
/// <summary>
/// PhoneNumber 属性。
/// </summary>
        public string? PhoneNumber { get; set; }
    }
}
