using LYBT.Shared.Models.Core;
using System;
using System.Collections.Generic;

namespace LYBT.WPF.Client.Core.Models.Configuration
{
    /// <summary>
    /// 治疗目录信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class TreatmentCatalogInfo : BaseTreatmentCatalog
    {
        /// <summary>父级名称（前端显示字段）</summary>
        public string ParentName { get; set; } = string.Empty;

        /// <summary>子项目列表（前端树形结构字段）</summary>
        public List<TreatmentCatalogInfo> Children { get; set; } = new();

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>是否展开（用于树形结构）</summary>
        public bool IsExpanded { get; set; }

        /// <summary>价格显示文本</summary>
        public string PriceDisplay => $"¥{Price:F2}";

        /// <summary>时长显示文本</summary>
        public string DurationDisplay => Duration.HasValue ? $"{Duration.Value}分钟" : "不限";

        /// <summary>完整路径（用于显示分类层级）</summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>是否为叶子节点</summary>
        public bool IsLeaf => Children.Count == 0;

        /// <summary>状态显示文本</summary>
        public string StatusDisplay => IsEnabled ? "启用" : "停用";

        /// <summary>预约要求显示文本</summary>
        public string AppointmentDisplay => RequireAppointment ? "需要预约" : "无需预约";
    }
}