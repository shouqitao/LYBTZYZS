using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.DiagnosisTreatment {

    /// <summary>
    /// 诊疗列表 DTO
    /// </summary>
    public class DiagnosisTreatmentDto {

        /// <summary>诊疗ID</summary>
        [DisplayName("诊疗ID")]
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>诊疗时间</summary>
        [DisplayName("诊疗时间")]
        public DateTime CreateTime { get; set; }
    }
}