using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方药材组成项输入DTO - 统一创建和更新
    /// Phase 3: 合并FormulaHerbItemCreateDto和FormulaHerbItemUpdateDto
    /// 支持延迟绑定：HerbId可空
    /// Issue #2014: 添加HerbName/Unit/ProcessingMethod字段
    /// </summary>
    public class FormulaHerbItemInputDto
    {
        /// <summary>项ID（更新时可填，创建时为null）</summary>
        public Guid? Id { get; set; }

        /// <summary>药材ID（可空，支持延迟绑定）</summary>
        public Guid? HerbId { get; set; }

        /// <summary>药材名称（必填）⭐ Issue #2014新增</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称最多100个字符")]
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>用量（必填，整数克）</summary>
        [Required(ErrorMessage = "用量不能为空")]
        [Range(1, 500, ErrorMessage = "用量必须在1~500之间")]
        [DisplayName("用量")]
        public int Dosage { get; set; }

        /// <summary>单位（必填，默认"g"）⭐ Issue #2014新增</summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(10, ErrorMessage = "单位最多10个字符")]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        /// <summary>炮制方法（可选）</summary>
        [StringLength(50, ErrorMessage = "炮制方法最多50个字符")]
        [DisplayName("炮制方法")]
        public string? Preparation { get; set; }

        /// <summary>加工方法（可选）⭐ Issue #2014新增</summary>
        [StringLength(100, ErrorMessage = "加工方法最多100个字符")]
        [DisplayName("加工方法")]
        public string? ProcessingMethod { get; set; }

        /// <summary>用法（可选）</summary>
        [StringLength(100, ErrorMessage = "用法最多100个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>排序（默认0）</summary>
        [DisplayName("排序")]
        public int SortOrder { get; set; } = 0;

        /// <summary>煎法（先煎、后下等）</summary>
        [DisplayName("煎法")]
        public DecocteMethod DecocteMethod { get; set; } = DecocteMethod.Default;
    }
}
