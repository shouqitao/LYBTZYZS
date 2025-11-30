using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Consultation
{

    /// <summary>
    /// 诊疗信息DTO - 简化版（Issue #1562 Phase 2）
    /// 与Consultation实体对齐，仅包含四诊信息和基础字段
    /// 移除了时间跟踪字段（StartTime/EndTime）和工作流状态（ConsultationStatus）
    /// DD-002: 移除Status字段，Consultation状态从聚合根MedicalCase派生
    /// </summary>
    public class ConsultationDto : TimestampDto, IRemarkable
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

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>望诊结果</summary>
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊结果</summary>
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊结果</summary>
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊结果</summary>
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>中医诊断</summary>
        [DisplayName("中医诊断")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        public string? Remark { get; set; }
    }

    // Issue #1562 Phase 2: 已删除 ConsultationDetailDto（与ConsultationDto重复，MedicalAdvice已合并）

    /// <summary>
    /// 诊疗输入DTO - 统一创建和更新
    /// Phase 3: 合并ConsultationCreateDto和ConsultationUpdateDto
    /// </summary>
    public class ConsultationInputDto
    {
        /// <summary>主诉</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "主诉长度不能超过{1}个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(ValidationConstants.LongRemarkMaxLength, ErrorMessage = "现病史长度不能超过{1}个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>望诊结果</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "望诊结果长度不能超过{1}个字符")]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊结果</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "闻诊结果长度不能超过{1}个字符")]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊结果</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "问诊结果长度不能超过{1}个字符")]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊结果</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "切诊结果长度不能超过{1}个字符")]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>中医诊断</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "中医诊断长度不能超过{1}个字符")]
        [DisplayName("中医诊断")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "治疗原则长度不能超过{1}个字符")]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(ValidationConstants.LongRemarkMaxLength, ErrorMessage = "医嘱长度不能超过{1}个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>
        /// 医案备注（保存诊断时同时更新MedicalCase.Remark）
        /// OpenSpec: clarify-cancel-consultation-logic - 统一在诊断保存时更新
        /// </summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "医案备注长度不能超过{1}个字符")]
        [DisplayName("医案备注")]
        public string? MedicalCaseRemark { get; set; }

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

        /// <summary>患者姓名（展示用）</summary>
        [DisplayName("患者姓名")]
        public string? PatientName { get; set; }

        /// <summary>医生姓名（展示用）</summary>
        [DisplayName("医生姓名")]
        public string? DoctorName { get; set; }
    }

    /// <summary>
    /// 诊疗验证结果
    /// </summary>
    public class ConsultationValidationResult
    {
        /// <summary>是否有效</summary>
        public bool IsValid { get; set; }

        /// <summary>错误消息</summary>
        public List<string> ErrorMessages { get; set; } = new();
    }

}
