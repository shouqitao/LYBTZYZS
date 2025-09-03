using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Workbench.Core
{
    /// <summary>
    /// 导航项模型
    /// </summary>
    public class NavigationItem
    {
        /// <summary>
        /// 导航项ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 图标名称或路径
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// 导航目标视图名称
        /// </summary>
        public string ViewName { get; set; } = string.Empty;

        /// <summary>
        /// 所属模块
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 子导航项
        /// </summary>
        public List<NavigationItem> Children { get; set; }

        /// <summary>
        /// 必需的权限
        /// </summary>
        public List<string> RequiredPermissions { get; set; }

        /// <summary>
        /// 工具提示
        /// </summary>
        public string ToolTip { get; set; } = string.Empty;

        /// <summary>
        /// 导航参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// 是否为分隔符
        /// </summary>
        public bool IsSeparator { get; set; }

        /// <summary>
        /// 徽章文本（用于显示数字或状态）
        /// </summary>
        public string BadgeText { get; set; } = string.Empty;

        /// <summary>
        /// 徽章类型（info, warning, error, success）
        /// </summary>
        public string BadgeType { get; set; } = string.Empty;

        public NavigationItem()
        {
            Children = new List<NavigationItem>();
            RequiredPermissions = new List<string>();
            Parameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// 创建分隔符
        /// </summary>
        public static NavigationItem CreateSeparator()
        {
            return new NavigationItem
            {
                IsSeparator = true,
                Id = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// 检查是否有子项
        /// </summary>
        public bool HasChildren => Children != null && Children.Count > 0;
    }
}