using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.FormulaTemplates
{
    /// <summary>
    /// 验方模板DTO
    /// </summary>
    public class FormulaTemplateDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>分类</summary>
        public string? Category { get; set; }

        /// <summary>描述</summary>
        public string? Description { get; set; }

        /// <summary>适应症</summary>
        public string? Indications { get; set; }

        /// <summary>用法用量</summary>
        public string? Usage { get; set; }

        /// <summary>药材列表</summary>
        public List<FormulaTemplateHerbDto> Herbs { get; set; } = new();

        /// <summary>是否常用</summary>
        public bool IsFrequent { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>创建人</summary>
        public string? CreatedBy { get; set; }

        /// <summary>更新人</summary>
        public string? UpdatedBy { get; set; }

        /// <summary>使用次数</summary>
        public int UsageCount { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 验方模板药材DTO
    /// </summary>
    public class FormulaTemplateHerbDto
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>剂量</summary>
        public decimal Dosage { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "g";

        /// <summary>用法说明</summary>
        public string? Usage { get; set; }

        /// <summary>排序</summary>
        public int Sort { get; set; }
    }
}