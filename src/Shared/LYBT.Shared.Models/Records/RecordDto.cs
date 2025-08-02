using System;

namespace LYBT.Shared.Models.Records
{
    /// <summary>
    /// 病例DTO
    /// </summary>
    public class RecordDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>病例编号</summary>
        public string RecordNo { get; set; } = string.Empty;

        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>患者性别</summary>
        public string PatientGender { get; set; } = string.Empty;

        /// <summary>患者年龄</summary>
        public int PatientAge { get; set; }

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>科室</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        public string PresentIllness { get; set; } = string.Empty;

        /// <summary>既往史</summary>
        public string PastHistory { get; set; } = string.Empty;

        /// <summary>过敏史</summary>
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>体格检查</summary>
        public string PhysicalExamination { get; set; } = string.Empty;

        /// <summary>中医诊断</summary>
        public string TCMDiagnosis { get; set; } = string.Empty;

        /// <summary>西医诊断</summary>
        public string WesternDiagnosis { get; set; } = string.Empty;

        /// <summary>治疗方案</summary>
        public string Treatment { get; set; } = string.Empty;

        /// <summary>处方ID</summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>就诊时间</summary>
        public DateTime VisitTime { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>状态（0:草稿 1:已完成 2:已归档）</summary>
        public int Status { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}