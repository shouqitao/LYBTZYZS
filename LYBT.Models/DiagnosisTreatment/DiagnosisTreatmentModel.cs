namespace LYBT.Models.DiagnosisTreatment {

    /// <summary>
    /// 诊疗主表实体
    /// </summary>
    public class DiagnosisTreatmentModel {

        /// <summary>
        /// 诊疗ID（主键）
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 病人ID（外键）
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 主诉
        /// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// 现病史（结构化文本）
        /// </summary>
        public string? PresentIllness { get; set; }

        /// <summary>
        /// 诊断类型ID（引用诊断目录，可扩展）
        /// </summary>
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>
        /// 诊断内容
        /// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 治疗项目（如针灸、正骨等）
        /// </summary>
        public List<TreatmentItemModel> Treatments { get; set; } = new();

        /// <summary>
        /// 本次形成的独立治疗药方
        /// </summary>
        public FormulaModel? Formula { get; set; }

        /// <summary>
        /// 诊疗创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}