using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.WPF.Client.Core.Models.Herbs;

namespace LYBT.WPF.Client.Core.Models.FormulaTemplates
{
    /// <summary>
    /// 验方模板信息模型
    /// </summary>
    public class FormulaTemplateInfo
    {
        /// <summary>模板ID</summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        public List<HerbInfo> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>是否启用</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>药材数量</summary>
        public int HerbCount => Herbs?.Count ?? 0;

        /// <summary>药材名称列表（用于显示）</summary>
        public string HerbNames => Herbs?.Count > 0 ? string.Join("、", Herbs.Take(3).Select(h => h.Name)) + (Herbs.Count > 3 ? "..." : "") : "无";
    }
}