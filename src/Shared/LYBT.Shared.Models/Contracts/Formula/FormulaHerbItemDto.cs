using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方中药材组成项DTO - 继承基础DTO提供ID
    /// 支持延迟绑定：允许先保存原始药材名称，稍后再绑定到药材库
    /// </summary>
    public class FormulaHerbItemDto : BaseDto
    {
        /// <summary>
        /// 药材ID（可空，支持延迟绑定）
        /// </summary>
        [DisplayName("中药材ID")]
        public Guid? HerbId { get; set; }

        /// <summary>
        /// 原始药材名称（从老系统导入时保存，用于延迟绑定）
        /// </summary>
        [DisplayName("原始药材名称")]
        public string? OriginalHerbName { get; set; }

        /// <summary>
        /// 是否已验证绑定（true表示HerbId已绑定到药材库）
        /// </summary>
        [DisplayName("已验证")]
        public bool IsValidated { get; set; }

        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("用量")]
        public int Dosage { get; set; }

        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("炮制方法")]
        public string? Preparation { get; set; }

        [DisplayName("加工方法")]
        public string? Processing { get => ProcessingMethod; set => ProcessingMethod = value; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("价格")]
        public decimal Price { get; set; }

        [DisplayName("单价")]
        public decimal UnitPrice => Price;

        [DisplayName("加工方法")]
        public string? ProcessingMethod { get; set; }

        [DisplayName("特殊说明")]
        public string? SpecialInstructions { get; set; }

        [DisplayName("排序")]
        public int SortOrder { get; set; }

        /// <summary>煎法（先煎、后下等）</summary>
        [DisplayName("煎法")]
        public DecocteMethod DecocteMethod { get; set; } = DecocteMethod.Default;

        // UltraThink导航属性 - 确保架构统一

        /// <summary>中药材导航属性</summary>
        [DisplayName("中药材")]
        public HerbDetailDto? Herb { get; set; }
    }
}
