using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方输入DTO - 统一创建和更新
    /// Phase 3: 合并FormulaCreateDto和FormulaUpdateDto
    /// OpenSpec: refactor-dto-simplification - 移除接口继承，直接声明Remark字段
    /// </summary>
    public class FormulaInputDto
    {

        [Required(ErrorMessage = "验方名称不能为空")]
        [StringLength(100, ErrorMessage = "验方名称不能超过100个字符")]
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "功效描述不能超过200个字符")]
        [DisplayName("功效")]
        public string Effect { get; set; } = string.Empty;
        [StringLength(1000, ErrorMessage = "验方描述不能超过1000个字符")]
        [DisplayName("验方描述")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "用法描述不能超过200个字符")]
        [DisplayName("用法")]
        public string Usage { get; set; } = string.Empty;
        [StringLength(200, ErrorMessage = "性味归经不能超过200个字符")]
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [StringLength(100, ErrorMessage = "验方分类不能超过100个字符")]
        [DisplayName("验方分类")]
        public string? Category { get; set; }

        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        [StringLength(500, ErrorMessage = "用药指导不能超过500个字符")]
        [DisplayName("用药指导")]
        public string? Instructions { get; set; }

        [StringLength(500, ErrorMessage = "主治症状不能超过500个字符")]
        [DisplayName("主治症状")]
        public string? Indications { get; set; }

        [StringLength(500, ErrorMessage = "禁忌症不能超过500个字符")]
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        [StringLength(200, ErrorMessage = "制备方法不能超过200个字符")]
        [DisplayName("制备方法")]
        public string? Preparation { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>验方ID（更新时必填，创建时为null）</summary>
        [DisplayName("验方ID")]
        public Guid? Id { get; set; }

        // OpenSpec: refactor-dto-simplification - Status字段已移除
        // InputDto不应包含Status字段，状态变更应通过专用API进行

        /// <summary>中药材组成</summary>
        [Required(ErrorMessage = "必须包含至少一味中药材")]
        [DisplayName("中药材组成")]
        public List<FormulaHerbItemInputDto> Herbs { get; set; } = new();
    }
}
