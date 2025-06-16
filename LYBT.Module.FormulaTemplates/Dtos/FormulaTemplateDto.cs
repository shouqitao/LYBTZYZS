using System;

namespace LYBT.Module.FormulaTemplates.Dtos {
    /// <summary>
    /// 经验方模板列表 DTO
    /// </summary>
    public class FormulaTemplateDto {
        /// <summary>模板ID</summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        public string Name { get; set; } = string.Empty;
    }
}
