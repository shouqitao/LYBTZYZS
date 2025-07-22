using System.ComponentModel;
namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 诊疗详情 DTO
    /// </summary>
    public class DiagnosisTreatmentDetailDto {

        /// <summary>诊疗ID</summary>
        [DisplayName("诊疗ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
/// <summary>
/// ChiefComplaint 属性。
/// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
/// <summary>
/// PresentIllness 属性。
/// </summary>
        public string? PresentIllness { get; set; }

        /// <summary>诊断类型ID</summary>
        [DisplayName("诊断类型ID")]
/// <summary>
/// DiagnosisCatalogId 属性。
/// </summary>
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>治疗项目</summary>
        [DisplayName("治疗项目")]
/// <summary>
/// Treatments 属性。
/// </summary>
        public List<TreatmentItemDto> Treatments { get; set; } = new();

        /// <summary>治疗方</summary>
        [DisplayName("治疗方")]
/// <summary>
/// Formula 属性。
/// </summary>
        public FormulaDto? Formula { get; set; }

        /// <summary>诊疗时间</summary>
        [DisplayName("诊疗时间")]
/// <summary>
/// CreateTime 属性。
/// </summary>
        public DateTime CreateTime { get; set; }
    }
}
