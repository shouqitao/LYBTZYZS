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

        /// <summary>分类</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>适应症</summary>
        public string? Indications { get; set; }

        /// <summary>用法</summary>
        public string? Usage { get; set; }

        /// <summary>剂量</summary>
        public string? Dosage { get; set; }

        /// <summary>禁忌</summary>
        public string? Contraindications { get; set; }

        /// <summary>来源</summary>
        public string? Source { get; set; }

        /// <summary>药材组成</summary>
        public List<FormulaHerbItem> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>创建人</summary>
        public string? CreatedBy { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>是否启用</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>药材数量</summary>
        public int HerbCount => Herbs?.Count ?? 0;

        /// <summary>总价格</summary>
        public decimal TotalPrice => Herbs?.Sum(h => h.SubTotal) ?? 0;

        /// <summary>药材名称列表（用于显示）</summary>
        public string HerbNames => Herbs?.Count > 0 ? string.Join("、", Herbs.Take(3).Select(h => h.HerbName)) + (Herbs.Count > 3 ? "..." : "") : "无";
    }

    /// <summary>
    /// 验方中的药材项
    /// </summary>
    public class FormulaHerbItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Dosage { get; set; }
        public string Unit { get; set; } = "g";
        public decimal UnitPrice { get; set; }
        public string? ProcessingMethod { get; set; }
        public string? SpecialInstructions { get; set; }
        public decimal SubTotal => Dosage * UnitPrice;
    }
}