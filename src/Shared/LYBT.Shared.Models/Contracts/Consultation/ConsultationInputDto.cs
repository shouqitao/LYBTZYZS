using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 诊疗输入DTO - 统一创建和更新
    /// Phase 3: 合并ConsultationCreateDto和ConsultationUpdateDto
    /// OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
    /// OpenSpec: refactor-dto-simplification - 移除展示字段(PatientName/DoctorName)
    /// </summary>
    /// <remarks>
    /// InputDto设计原则：
    /// - 只含可写字段，排除展示字段
    /// - PatientName/DoctorName移至ConsultationDetailDto/ConsultationListDto
    /// - Id可空：null=创建，有值=更新
    /// </remarks>
    public class ConsultationInputDto
    {
        // 诊断核心字段（精简版）

        /// <summary>现病史</summary>
        [StringLength(ValidationConstants.FourDiagnosisMaxLength, ErrorMessage = "现病史长度不能超过{1}个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>舌诊</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "舌诊长度不能超过{1}个字符")]
        [DisplayName("舌诊")]
        public string? TongueDiagnosis { get; set; }

        /// <summary>脉诊</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "脉诊长度不能超过{1}个字符")]
        [DisplayName("脉诊")]
        public string? PulseDiagnosis { get; set; }

        /// <summary>中医诊断（必填）</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "中医诊断长度不能超过{1}个字符")]
        [DisplayName("中医诊断")]
        public string? TCMDiagnosis { get; set; }

        // 系统字段

        /// <summary>诊疗ID（更新时必填，创建时为null，共享主键=MedicalCaseId）</summary>
        [DisplayName("诊疗ID")]
        public Guid? Id { get; set; }

        /// <summary>医疗案例ID（创建时必填，共享主键）</summary>
        [DisplayName("医疗案例ID")]
        public Guid? MedicalCaseId { get; set; }

        /// <summary>患者ID（创建时从MedicalCase获取）</summary>
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        /// <summary>关联用户ID（医生，创建时必填）</summary>
        [DisplayName("关联用户ID")]
        public Guid? UserId { get; set; }

        // OpenSpec: refactor-dto-simplification - PatientName/DoctorName已移至ConsultationDetailDto/ConsultationListDto
    }
}
