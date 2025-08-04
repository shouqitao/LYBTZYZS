using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Contracts.FormulaTemplates
{
    /// <summary>
    /// 验方模板详情DTO
    /// </summary>
    public class FormulaTemplateDetailDto
    {
        /// <summary>模板ID</summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>分类</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>适应症</summary>
        public string? Indications { get; set; }

        /// <summary>功效</summary>
        public string? Efficacy { get; set; }

        /// <summary>用法用量</summary>
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>药材列表</summary>
        public List<FormulaTemplateHerbDto> Herbs { get; set; } = new();

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdateTime { get; set; }
    }
}