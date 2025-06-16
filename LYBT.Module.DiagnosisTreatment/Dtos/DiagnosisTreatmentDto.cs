using System;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {
    /// <summary>
    /// 诊疗列表 DTO
    /// </summary>
    public class DiagnosisTreatmentDto {
        /// <summary>诊疗ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>诊断内容</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>诊疗时间</summary>
        public DateTime CreateTime { get; set; }
    }
}
