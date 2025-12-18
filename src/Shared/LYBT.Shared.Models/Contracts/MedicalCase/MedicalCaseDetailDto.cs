using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医疗案例详情DTO - 聚合DTO（包含嵌套的Consultation和Prescription）
    /// OpenSpec: refactor-dto-simplification - 独立定义，不再继承MedicalCaseDto
    /// 用于Desktop端聚合根模式，需要嵌套的子实体DTO
    /// </summary>
    public class MedicalCaseDetailDto : TimestampDto, IRemarkable
    {
        // ========== 基础字段（原MedicalCaseDto字段） ==========

        [DisplayName("案例编号")]
        [StringLength(50, ErrorMessage = "案例编号长度不能超过50个字符")]
        public string? CaseNumber { get; set; }

        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("患者性别")]
        public string? PatientGender { get; set; }

        [DisplayName("患者年龄")]
        public int? PatientAge { get; set; }

        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        [DisplayName("诊断ID")]
        public Guid? ConsultationId { get; set; }

        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

        [DisplayName("诊疗时间")]
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        /// <summary>医疗案例专用状态</summary>
        [DisplayName("案例状态")]
        public MedicalCaseStatus CaseStatus { get; set; } = MedicalCaseStatus.Active;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        /// <summary>中医诊断信息</summary>
        [DisplayName("诊断")]
        [StringLength(500, ErrorMessage = "诊断信息长度不能超过500个字符")]
        public string? Diagnosis { get; set; }

        /// <summary>是否有诊疗记录（计算属性）</summary>
        public bool HasConsultation => ConsultationId.HasValue;

        /// <summary>是否有处方（计算属性）</summary>
        public bool HasPrescription => PrescriptionId.HasValue;

        // ========== 扩展字段 ==========

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        // ========== 聚合DTO嵌套属性 ==========

        /// <summary>诊疗记录详情（嵌套DTO）</summary>
        [DisplayName("诊疗记录")]
        public ConsultationDetailDto? Consultation { get; set; }

        /// <summary>处方详情（嵌套DTO）</summary>
        [DisplayName("处方")]
        public PrescriptionDetailDto? Prescription { get; set; }
    }
}
