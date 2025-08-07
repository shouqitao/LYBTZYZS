using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Formulas
{
    /// <summary>
    /// 更新验方模板DTO
    /// </summary>
    public class FormulaUpdateDto
    {
        /// <summary>模板ID</summary>
        [Required(ErrorMessage = "模板ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        [Required(ErrorMessage = "模板名称不能为空")]
        [StringLength(100, ErrorMessage = "模板名称长度不能超过100个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>分类</summary>
        [Required(ErrorMessage = "分类不能为空")]
        [StringLength(50, ErrorMessage = "分类长度不能超过50个字符")]
        public string Category { get; set; } = string.Empty;

        /// <summary>适应症</summary>
        [Required(ErrorMessage = "适应症不能为空")]
        [StringLength(500, ErrorMessage = "适应症长度不能超过500个字符")]
        public string Indications { get; set; } = string.Empty;

        /// <summary>功效</summary>
        [StringLength(500, ErrorMessage = "功效长度不能超过500个字符")]
        public string? Efficacy { get; set; }

        /// <summary>用法用量</summary>
        [StringLength(500, ErrorMessage = "用法用量长度不能超过500个字符")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        public string? Remark { get; set; }

        /// <summary>药材列表</summary>
        [Required(ErrorMessage = "药材列表不能为空")]
        public List<FormulaHerbDto> Herbs { get; set; } = new();
    }
}