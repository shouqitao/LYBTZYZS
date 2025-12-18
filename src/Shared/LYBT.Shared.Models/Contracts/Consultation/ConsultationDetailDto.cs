using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 诊疗详情DTO - 简化版（Issue #1562 Phase 2）
    /// OpenSpec: refactor-dto-simplification - 重命名为ConsultationDetailDto符合DTO规范
    /// 与Consultation实体对齐，仅包含四诊信息和基础字段
    /// 移除了时间跟踪字段（StartTime/EndTime）和工作流状态（ConsultationStatus）
    /// DD-002: 移除Status字段，Consultation状态从聚合根MedicalCase派生
    /// </summary>
    public class ConsultationDetailDto : TimestampDto
    {
        /// <summary>医疗案例ID（共享主键）</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID（从MedicalCase获取）</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>患者姓名（展示用）</summary>
        [DisplayName("患者姓名")]
        public string? PatientName { get; set; }

        /// <summary>医生姓名（展示用）</summary>
        [DisplayName("医生姓名")]
        public string? DoctorName { get; set; }

        // 诊断核心字段（精简版 - OpenSpec: refactor-diagnosis-fields）

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>舌诊</summary>
        [DisplayName("舌诊")]
        public string? TongueDiagnosis { get; set; }

        /// <summary>脉诊</summary>
        [DisplayName("脉诊")]
        public string? PulseDiagnosis { get; set; }

        /// <summary>中医诊断（必填）</summary>
        [DisplayName("中医诊断")]
        public string? TCMDiagnosis { get; set; }
    }
}
