using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 看诊详情DTO
    /// </summary>
    public class ConsultationDetailDto : ConsultationDto
    {
        /// <summary>现病史</summary>
        public string PresentIllness { get; set; } = string.Empty;

        /// <summary>体格检查</summary>
        public string PhysicalExamination { get; set; } = string.Empty;

        /// <summary>治疗建议</summary>
        public string TreatmentAdvice { get; set; } = string.Empty;

        /// <summary>用药医嘱</summary>
        public string MedicationInstructions { get; set; } = string.Empty;

        /// <summary>生活医嘱</summary>
        public string LifestyleInstructions { get; set; } = string.Empty;

        /// <summary>复诊建议</summary>
        public string FollowUpAdvice { get; set; } = string.Empty;

        /// <summary>处方明细</summary>
        public List<PrescriptionItemDto> PrescriptionItems { get; set; } = new();
    }

    /// <summary>
    /// 处方项DTO
    /// </summary>
    public class PrescriptionItemDto
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>用量</summary>
        public decimal Dosage { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>用法</summary>
        public string Usage { get; set; } = string.Empty;

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>小计</summary>
        public decimal SubTotal { get; set; }
    }
}