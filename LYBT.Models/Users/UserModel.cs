using LYBT.Common.Enums.Users;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Users.Models {

    /// <summary>
    /// 用户实体类，数据库映射
    /// </summary>
    public class UserModel {

        /// <summary>
        /// 用户唯一标识（主键）
        /// </summary>
        [DisplayName("用户ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 用户名（唯一）
        /// </summary>
        [Required, StringLength(32, MinimumLength = 2)]
        [DisplayName("用户名")]
/// <summary>
/// UserName 属性。
/// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名
        /// </summary>
        [Required, StringLength(20)]
        [DisplayName("真实姓名")]
/// <summary>
/// RealName 属性。
/// </summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名拼音码
        /// </summary>
        [StringLength(32)]
        [DisplayName("拼音码")]
/// <summary>
/// PinyinCode 属性。
/// </summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>
        /// 用户拥有的所有角色
        /// </summary>
        [Required]
        [DisplayName("角色列表")]
/// <summary>
/// Roles 属性。
/// </summary>
        public List<UserRole> Roles { get; set; } = new();

        /// <summary>
        /// 启用状态（true=启用，false=禁用）
        /// </summary>
        [DisplayName("是否启用")]
/// <summary>
/// IsActive 属性。
/// </summary>
        public bool IsActive { get; set; } = true;

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
        /// 密码哈希（不可在DTO中暴露）
        /// </summary>
        [Required]
        [DisplayName("密码哈希")]
/// <summary>
/// PasswordHash 属性。
/// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 连续登录失败次数
        /// </summary>
        [DisplayName("登录失败次数")]
/// <summary>
/// FailedLoginCount 属性。
/// </summary>
        public int FailedLoginCount { get; set; } = 0;

        /// <summary>
        /// 账号锁定截止时间（null为未锁定）
        /// </summary>
        [DisplayName("锁定截止")]
/// <summary>
/// LockoutEnd 属性。
/// </summary>
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [EmailAddress]
        [DisplayName("邮箱")]
/// <summary>
/// Email 属性。
/// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        [Phone]
        [DisplayName("手机号")]
/// <summary>
/// PhoneNumber 属性。
/// </summary>
        public string? PhoneNumber { get; set; }
    }
}
