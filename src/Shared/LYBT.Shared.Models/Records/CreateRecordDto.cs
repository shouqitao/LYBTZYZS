using System;

namespace LYBT.Shared.Models.Records
{
    /// <summary>
    /// 创建病例DTO
    /// </summary>
    public class CreateRecordDto
    {
        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>科室</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        public string PresentIllness { get; set; } = string.Empty;

        /// <summary>既往史</summary>
        public string? PastHistory { get; set; }

        /// <summary>过敏史</summary>
        public string? AllergyHistory { get; set; }

        /// <summary>家族史</summary>
        public string? FamilyHistory { get; set; }

        /// <summary>个人史</summary>
        public string? PersonalHistory { get; set; }

        /// <summary>月经史（女性）</summary>
        public string? MenstrualHistory { get; set; }

        /// <summary>婚育史</summary>
        public string? MaritalHistory { get; set; }

        /// <summary>体格检查</summary>
        public string? PhysicalExamination { get; set; }

        /// <summary>望诊</summary>
        public string? Inspection { get; set; }

        /// <summary>闻诊</summary>
        public string? Auscultation { get; set; }

        /// <summary>问诊</summary>
        public string? Inquiry { get; set; }

        /// <summary>切诊</summary>
        public string? Palpation { get; set; }

        /// <summary>舌诊</summary>
        public string? TongueExamination { get; set; }

        /// <summary>脉诊</summary>
        public string? PulseExamination { get; set; }

        /// <summary>辨证</summary>
        public string? SyndromeDifferentiation { get; set; }

        /// <summary>治法</summary>
        public string? TreatmentPrinciple { get; set; }

        /// <summary>中医诊断</summary>
        public string TCMDiagnosis { get; set; } = string.Empty;

        /// <summary>西医诊断</summary>
        public string? WesternDiagnosis { get; set; }

        /// <summary>治疗方案</summary>
        public string Treatment { get; set; } = string.Empty;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}