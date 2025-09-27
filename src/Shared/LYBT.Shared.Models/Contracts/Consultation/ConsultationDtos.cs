using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Consultation
{

    /// <summary>
    /// 诊疗信息DTO - UltraThink v2.0简化版
    /// 与Consultation实体对齐，实现四诊信息管理
    /// </summary>
    public class ConsultationDto : StatusDto, IRemarkable
    {

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string? PatientName { get; set; }

        /// <summary>医生姓名</summary>
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

        /// <summary>诊疗开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>诊疗结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>诊疗状态</summary>
        [DisplayName("诊疗状态")]
        public ConsultationStatus ConsultationStatus { get; set; } = ConsultationStatus.InProgress;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        public string? Remark { get; set; }

    }

    /// <summary>
    /// 诊疗详情DTO - 包含完整的四诊信息
    /// </summary>
    public class ConsultationDetailDto : TimestampDto, IRemarkable
    {

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string? PatientName { get; set; }

        /// <summary>医生姓名</summary>
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

        /// <summary>诊疗开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>诊疗结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>诊疗持续时间(分钟)</summary>
        [DisplayName("持续时间")]
        public int Duration => EndTime.HasValue ? (int)(EndTime.Value - StartTime).TotalMinutes : 0;

        /// <summary>诊疗状态</summary>
        [DisplayName("诊疗状态")]
        public ConsultationStatus ConsultationStatus { get; set; } = ConsultationStatus.InProgress;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        public string? Remark { get; set; }

        // 计算属性

        /// <summary>是否已完成</summary>
        public bool IsCompleted => ConsultationStatus == ConsultationStatus.Completed;
    }

    /// <summary>
    /// 诊疗输入基础DTO - 提取创建和更新的共同字段
    /// </summary>
    public abstract class ConsultationInputBaseDto : IRemarkable
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


        /// <summary>诊断结果</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "诊断结果长度不能超过{1}个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "治疗原则长度不能超过{1}个字符")]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(ValidationConstants.LongRemarkMaxLength, ErrorMessage = "医嘱长度不能超过{1}个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <inheritdoc/>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 诊疗创建DTO - 继承输入基础DTO
    /// </summary>
    public class ConsultationCreateDto : ConsultationInputBaseDto
    {
        /// <summary>医疗案例ID</summary>
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [Required(ErrorMessage = "关联用户ID不能为空")]
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>诊疗开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>患者姓名(展示用)</summary>
        [DisplayName("患者姓名")]
        public string? PatientName { get; set; }

        /// <summary>医生姓名(展示用)</summary>
        [DisplayName("医生姓名")]
        public string? DoctorName { get; set; }
    }

    /// <summary>
    /// 诊疗更新DTO - 继承输入基础DTO并实现ID接口
    /// </summary>
    public class ConsultationUpdateDto : ConsultationInputBaseDto, IIdentifiable<Guid>
    {
        /// <summary>诊疗ID</summary>
        [Required(ErrorMessage = "诊疗ID不能为空")]
        [DisplayName("诊疗ID")]
        public Guid Id { get; set; }

        /// <summary>诊疗状态</summary>
        [DisplayName("诊疗状态")]
        public ConsultationStatus? ConsultationStatus { get; set; }

        /// <summary>诊疗结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }
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
