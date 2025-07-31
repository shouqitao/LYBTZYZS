using System;
using LYBT.WPF.Client.Core.Enums;

namespace LYBT.WPF.Client.Core.Models.Users
{
    /// <summary>
    /// 用户查询请求
    /// </summary>
    public class UserQueryRequest
    {
        /// <summary>关键词</summary>
        public string? Keyword { get; set; }

        /// <summary>用户角色</summary>
        public UserRole? Role { get; set; }

        /// <summary>启用状态</summary>
        public bool? IsActive { get; set; }

        /// <summary>当前页码</summary>
        public int Page { get; set; } = 1;

        /// <summary>每页条数</summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 用户创建请求
    /// </summary>
    public class UserCreateRequest
    {
        /// <summary>用户名</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        public UserRole Role { get; set; }

        /// <summary>邮箱</summary>
        public string? Email { get; set; }

        /// <summary>电话</summary>
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// 用户更新请求
    /// </summary>
    public class UserUpdateRequest
    {
        /// <summary>用户ID</summary>
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        public UserRole Role { get; set; }

        /// <summary>邮箱</summary>
        public string? Email { get; set; }

        /// <summary>电话</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>是否启用</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 角色信息
    /// </summary>
    public class RoleInfo
    {
        /// <summary>角色值</summary>
        public UserRole Value { get; set; }

        /// <summary>角色名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>角色描述</summary>
        public string Description { get; set; } = string.Empty;
    }
}