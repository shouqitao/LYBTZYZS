using System.ComponentModel;
namespace LYBT.Models.DiagnosisTreatment {

    /// <summary>
    /// 诊疗主表实体
    /// </summary>
    public class DiagnosisTreatmentModel {

        /// <summary>
        /// 诊疗ID（主键）
        /// </summary>
        [DisplayName("诊疗ID（主键）")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 病人ID（外键）
        /// </summary>
        [DisplayName("病人ID（外键）")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 主诉
        /// </summary>
        [DisplayName("主诉")]
/// <summary>
/// ChiefComplaint 属性。
/// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// 现病史（结构化文本）
        /// </summary>
        [DisplayName("现病史（结构化文本）")]
/// <summary>
/// PresentIllness 属性。
/// </summary>
        public string? PresentIllness { get; set; }

        /// <summary>
        /// 诊断类型ID（引用诊断目录，可扩展）
        /// </summary>
        [DisplayName("诊断类型ID（引用诊断目录，可扩展）")]
/// <summary>
/// DiagnosisCatalogId 属性。
/// </summary>
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>
        /// 诊断内容
        /// </summary>
        [DisplayName("诊断内容")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 治疗项目（如针灸、正骨等）
        /// </summary>
        [DisplayName("治疗项目（如针灸、正骨等）")]
/// <summary>
/// Treatments 属性。
/// </summary>
        public List<TreatmentItemModel> Treatments { get; set; } = new();

        /// <summary>
        /// 本次形成的独立治疗药方
        /// </summary>
        [DisplayName("本次形成的独立治疗药方")]
/// <summary>
/// Formula 属性。
/// </summary>
        public FormulaModel? Formula { get; set; }

        /// <summary>
        /// 诊疗创建时间
        /// </summary>
        [DisplayName("诊疗创建时间")]
/// <summary>
/// CreateTime 属性。
/// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
