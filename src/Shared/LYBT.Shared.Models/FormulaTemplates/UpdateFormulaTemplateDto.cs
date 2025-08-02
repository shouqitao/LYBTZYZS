using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.FormulaTemplates
{
    /// <summary>
    /// 更新验方模板DTO
    /// </summary>
    public class UpdateFormulaTemplateDto
    {
        /// <summary>模板名称</summary>
        [Required(ErrorMessage = "模板名称不能为空")]
        [StringLength(100, ErrorMessage = "模板名称长度不能超过100个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>分类</summary>
        [StringLength(50, ErrorMessage = "分类长度不能超过50个字符")]
        public string? Category { get; set; }

        /// <summary>描述</summary>
        [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
        public string? Description { get; set; }

        /// <summary>适应症</summary>
        [StringLength(500, ErrorMessage = "适应症长度不能超过500个字符")]
        public string? Indications { get; set; }

        /// <summary>用法用量</summary>
        [StringLength(200, ErrorMessage = "用法用量长度不能超过200个字符")]
        public string? Usage { get; set; }

        /// <summary>药材列表</summary>
        [Required(ErrorMessage = "药材列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要添加一味药材")]
        public List<CreateFormulaTemplateHerbDto> Herbs { get; set; } = new();

        /// <summary>是否常用</summary>
        public bool IsFrequent { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }
}