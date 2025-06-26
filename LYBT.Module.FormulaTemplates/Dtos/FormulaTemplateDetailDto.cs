using LYBT.Module.Herbs.Dtos;

namespace LYBT.Module.FormulaTemplates.Dtos {

    /// <summary>
    /// 经验方模板详情 DTO
    /// </summary>
    public class FormulaTemplateDetailDto {

        /// <summary>模板ID</summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        public List<HerbDto> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}