using System.ComponentModel;
namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 诊疗列表 DTO
    /// </summary>
    public class DiagnosisTreatmentDto {

        /// <summary>诊疗ID</summary>
        [DisplayName("诊疗ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>诊疗时间</summary>
        [DisplayName("诊疗时间")]
/// <summary>
/// CreateTime 属性。
/// </summary>
        public DateTime CreateTime { get; set; }
    }
}
