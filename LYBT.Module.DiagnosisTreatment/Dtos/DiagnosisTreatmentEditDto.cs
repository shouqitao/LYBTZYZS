using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {
    /// <summary>
    /// 编辑诊疗记录 DTO
    /// </summary>
    public class DiagnosisTreatmentEditDto {
        [Required(ErrorMessage = "诊疗ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>主诉</summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }

        /// <summary>诊断类型ID</summary>
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>诊断内容</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>治疗项目</summary>
        public List<TreatmentItemDto> Treatments { get; set; } = new();

        /// <summary>治疗方</summary>
        public FormulaDto? Formula { get; set; }
    }
}
