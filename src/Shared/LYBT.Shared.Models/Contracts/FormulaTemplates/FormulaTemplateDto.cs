using System;

namespace LYBT.Shared.Models.Contracts.FormulaTemplates
{
    /// <summary>
    /// 验方模板列表DTO
    /// </summary>
    public class FormulaTemplateDto
    {
        /// <summary>模板ID</summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>分类</summary>
        public string? Category { get; set; }

        /// <summary>适应症</summary>
        public string? Indications { get; set; }

        /// <summary>药材数量</summary>
        public int HerbCount { get; set; }

        /// <summary>药材名称列表（逗号分隔）</summary>
        public string? HerbNames { get; set; }

        /// <summary>是否启用</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdateTime { get; set; }
    }
}