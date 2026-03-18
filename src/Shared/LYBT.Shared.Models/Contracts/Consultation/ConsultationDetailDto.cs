using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 诊疗详情DTO - 扁平化设计
    /// OpenSpec: refactor-dto-simplification - 移除继承，直接定义所有字段
    /// 与Consultation实体对齐，仅包含四诊信息和基础字段
    /// </summary>
    public class ConsultationDetailDto
    {
        // ========== 基础标识字段 ==========

        /// <summary>唯一标识符</summary>
        [DisplayName("ID")]
        public Guid Id { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>创建者ID</summary>
        [DisplayName("创建者")]
        public Guid? CreatedBy { get; set; }

        // ========== 关联字段 ==========

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

        // ========== 诊断核心字段 ==========

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
        public string? TcmDiagnosis { get; set; }
    }
}
