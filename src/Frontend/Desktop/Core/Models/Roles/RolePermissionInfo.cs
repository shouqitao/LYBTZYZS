using System;
using System.Collections.Generic;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Models.Roles
{
    /// <summary>
    /// 角色权限信息模型
    /// </summary>
    public class RolePermissionInfo 
    {
        /// <summary>角色枚举值</summary>
        public UserRole Role { get; set; }

        /// <summary>角色名称</summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>角色描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>可访问的模块列表</summary>
        public List<string> AccessibleModules { get; set; } = new();

        /// <summary>是否为系统角色（不可删除）</summary>
        public bool IsSystemRole { get; set; } = true;

        /// <summary>是否激活</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>该角色的用户数量</summary>
        public int UserCount { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>权限摘要</summary>
        public string PermissionSummary => AccessibleModules.Count > 0 
            ? $"{AccessibleModules.Count} 个模块权限" 
            : "无模块权限";

        /// <summary>状态描述</summary>
        public string StatusDescription => IsActive ? "启用" : "禁用";

        /// <summary>用户数量描述</summary>
        public string UserCountDescription => UserCount == 0 
            ? "暂无用户" 
            : $"{UserCount} 个用户";
    }
}