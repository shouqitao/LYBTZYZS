using System;
using System.Collections.Generic;

namespace LYBT.WPF.Client.Core.Models.Records
{
    /// <summary>
    /// 医疗记录（用于打印和传输）
    /// </summary>
    public class MedicalRecord
    {
        /// <summary>记录ID</summary>
        public Guid Id { get; set; }

        /// <summary>病历ID</summary>
        public Guid RecordId { get; set; }

        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>患者性别</summary>
        public string PatientGender { get; set; } = string.Empty;

        /// <summary>患者年龄</summary>
        public int PatientAge { get; set; }

        /// <summary>患者电话</summary>
        public string PatientPhone { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>诊断</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }

        /// <summary>治疗建议</summary>
        public string? TreatmentAdvice { get; set; }

        /// <summary>中药处方</summary>
        public List<FormulaIngredient>? HerbalFormula { get; set; }

        /// <summary>记录时间</summary>
        public DateTime RecordTime { get; set; }

        /// <summary>总金额</summary>
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// 处方成分
    /// </summary>
    public class FormulaIngredient
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>数量</summary>
        public decimal Quantity { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>用法</summary>
        public string Usage { get; set; } = string.Empty;
    }
}