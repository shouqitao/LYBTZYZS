using System;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 看诊信息DTO
    /// </summary>
    public class ConsultationDto
    {
        /// <summary>看诊ID</summary>
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>科室</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>诊断</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>看诊日期</summary>
        public DateTime ConsultationDate { get; set; }

        /// <summary>看诊时长（分钟）</summary>
        public int Duration { get; set; }

        /// <summary>费用总额</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }
    }
}