namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 诊疗详情 DTO
    /// </summary>
    public class DiagnosisTreatmentDetailDto {

        /// <summary>诊疗ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

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

        /// <summary>诊疗时间</summary>
        public DateTime CreateTime { get; set; }
    }
}