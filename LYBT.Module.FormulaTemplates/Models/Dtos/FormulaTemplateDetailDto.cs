using LYBT.Module.Herbs.Models.Dtos;
using System.ComponentModel;

namespace LYBT.Module.FormulaTemplates.Models.Dtos {

    /// <summary>
    /// 经验方模板详情 DTO
    /// </summary>
    public class FormulaTemplateDetailDto {

        /// <summary>模板ID</summary>
        [DisplayName("模板ID")]
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        [DisplayName("模板名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
        public List<HerbDto> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建者ID</summary>
        [DisplayName("创建者ID")]
        public Guid CreatedById { get; set; }

        /// <summary>创建者姓名</summary>
        [DisplayName("创建者姓名")]
        public string CreatedByName { get; set; } = string.Empty;

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; }

        /// <summary>共享时间</summary>
        [DisplayName("共享时间")]
        public DateTime? SharedAt { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; }
    }
}