using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Users
{
    /// <summary>
    /// 用户信息DTO - 前后端共享API契约
    /// 用于用户信息的展示和传输（不包含敏感信息）
    /// </summary>
    public class UserDto
    {
        /// <summary>用户ID</summary>
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [DisplayName("用户角色")]
        public UserRole Role { get; set; } = UserRole.DiagnosingDoctor;

        /// <summary>邮箱</summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>电话号码</summary>
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinyinCode { get; set; }

        /// <summary>五笔码</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>头像URL</summary>
        [DisplayName("头像")]
        public string? Avatar { get; set; }

        /// <summary>部门/科室</summary>
        [DisplayName("部门")]
        public string? Department { get; set; }

        /// <summary>职位</summary>
        [DisplayName("职位")]
        public string? Position { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>是否在线</summary>
        [DisplayName("是否在线")]
        public bool IsOnline { get; set; }

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>最后登录IP</summary>
        [DisplayName("最后登录IP")]
        public string? LastLoginIp { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}