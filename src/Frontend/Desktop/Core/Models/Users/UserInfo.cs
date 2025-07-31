using System;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Models.Users
{
    /// <summary>
    /// 用户信息模型 - 前端专用
    /// </summary>
    public class UserInfo
    {
        /// <summary>用户唯一标识</summary>
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        public UserRole Role { get; set; }

        /// <summary>账号启用状态</summary>
        public bool IsActive { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>最近登录时间</summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>邮箱地址</summary>
        public string? Email { get; set; }

        /// <summary>联系电话</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>是否有超级管理员权限</summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>是否有管理员权限</summary>
        public bool IsAdmin => Role == UserRole.Admin;

        /// <summary>是否有医生权限</summary>
        public bool IsDoctor => Role == UserRole.DiagnosingDoctor;
    }
}