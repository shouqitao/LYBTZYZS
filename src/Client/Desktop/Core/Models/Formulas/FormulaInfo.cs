using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Shared.Models.Core;
using LYBT.Desktop.Core.Models.Herbs;

namespace LYBT.Desktop.Core.Models.Formulas
{
    /// <summary>
    /// 验方信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class FormulaInfo : BaseFormula
    {
        /// <summary>分类</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>适应症</summary>
        public string? Indications { get; set; }

        /// <summary>用法用量说明</summary>
        public string? DosageInstruction { get; set; }

        /// <summary>禁忌</summary>
        public string? Contraindications { get; set; }

        /// <summary>来源</summary>
        public string? Source { get; set; }

        /// <summary>药材组成</summary>
        public List<FormulaHerbItem> Herbs { get; set; } = new();

        /// <summary>药材组成（Items别名，与Herbs相同）</summary>
        public List<FormulaHerbItem> Items => Herbs;

        /// <summary>描述信息（映射到Remark）</summary>
        public string? Description 
        { 
            get => Remark; 
            set => Remark = value; 
        }

        /// <summary>创建人</summary>
        public string? CreatedBy { get; set; }

        /// <summary>药材数量</summary>
        public int HerbCount => Herbs?.Count ?? 0;

        /// <summary>总价格</summary>
        public decimal TotalPrice => Herbs?.Sum(h => h.SubTotal) ?? 0;

        /// <summary>药材名称列表（用于显示）</summary>
        public string HerbNames => Herbs?.Count > 0 ? string.Join("、", Herbs.Take(3).Select(h => h.HerbName)) + (Herbs.Count > 3 ? "..." : "") : "无";

        /// <summary>
        /// 创建时间（前端显示字段，映射自CreateTime）
        /// </summary>
        public DateTime CreatedTime
        {
            get => CreateTime;
            set => CreateTime = value;
        }

        /// <summary>
        /// 更新时间（前端显示字段，映射自UpdateTime）
        /// </summary>
        public DateTime? UpdatedTime
        {
            get => UpdateTime;
            set => UpdateTime = value;
        }
    }

    /// <summary>
    /// 验方中的药材项
    /// </summary>
    public class FormulaHerbItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "g";
        public decimal UnitPrice { get; set; }
        public string? ProcessingMethod { get; set; }
        public string? SpecialInstructions { get; set; }
        public decimal SubTotal => Quantity * UnitPrice;
    }
}