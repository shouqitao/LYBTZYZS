using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Consultation
{

    /// <summary>
    /// 看诊信息DTO - UltraThink v2.0简化版
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

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

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

        /// <summary>诊断结果</summary>
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>看诊开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>看诊结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>看诊状态</summary>
        [DisplayName("看诊状态")]
        public ConsultationStatus ConsultationStatus { get; set; } = ConsultationStatus.InProgress;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        public string? Remark { get; set; }

        // 兼容性字段

        /// <summary>看诊时间（兼容别名）</summary>
        public DateTime ConsultationTime => StartTime;

        /// <summary>听诊（兼容别名）</summary>
        public string? Auscultation => AuscultationOlfaction;
    }

    /// <summary>
    /// 看诊详情DTO - 包含完整的四诊信息
    /// </summary>
    public class ConsultationDetailDto : TimestampDto, IRemarkable
    {

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

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

        /// <summary>诊断结果</summary>
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [DisplayName("医嘱")]
        public string? DoctorAdvice { get; set; }

        /// <summary>看诊开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>看诊结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>看诊持续时间(分钟)</summary>
        [DisplayName("持续时间")]
        public int Duration => EndTime.HasValue ? (int)(EndTime.Value - StartTime).TotalMinutes : 0;

        /// <summary>看诊状态</summary>
        [DisplayName("看诊状态")]
        public ConsultationStatus ConsultationStatus { get; set; } = ConsultationStatus.InProgress;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        public string? Remark { get; set; }

        // 兼容性字段

        /// <summary>医嘱（兼容别名）</summary>
        public string? MedicalAdvice => DoctorAdvice;

        /// <summary>状态（兼容别名）</summary>
        public ConsultationStatus Status => ConsultationStatus;

        /// <summary>是否已完成</summary>
        public bool IsCompleted => ConsultationStatus == ConsultationStatus.Completed;

        /// <summary>用户ID（兼容别名）</summary>
        public Guid UserId => DoctorId;

        /// <summary>看诊时间（兼容别名）</summary>
        public DateTime ConsultationTime => StartTime;
    }

    /// <summary>
    /// 看诊输入基础DTO - 提取创建和更新的共同字段
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

        /// <summary>舌诊结果</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "舌诊结果长度不能超过{1}个字符")]
        [DisplayName("舌诊")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊结果</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "脉诊结果长度不能超过{1}个字符")]
        [DisplayName("脉诊")]
        public string? PulseCondition { get; set; }

        /// <summary>辨证分析</summary>
        [StringLength(800, ErrorMessage = "辨证分析长度不能超过800个字符")]
        [DisplayName("辨证分析")]
        public string? PatternDifferentiation { get; set; }

        /// <summary>中医诊断</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "中医诊断长度不能超过{1}个字符")]
        [DisplayName("中医诊断")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>西医诊断</summary>
        [StringLength(ValidationConstants.DiagnosisMaxLength, ErrorMessage = "西医诊断长度不能超过{1}个字符")]
        [DisplayName("西医诊断")]
        public string? WesternDiagnosis { get; set; }

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
        public string? DoctorAdvice { get; set; }

        /// <inheritdoc/>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 看诊创建DTO - 继承输入基础DTO
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

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>看诊开始时间</summary>
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
    /// 看诊更新DTO - 继承输入基础DTO并实现ID接口
    /// </summary>
    public class ConsultationUpdateDto : ConsultationInputBaseDto, IIdentifiable<Guid>
    {
        /// <summary>看诊ID</summary>
        [Required(ErrorMessage = "看诊ID不能为空")]
        [DisplayName("看诊ID")]
        public Guid Id { get; set; }

        /// <summary>看诊状态</summary>
        [DisplayName("看诊状态")]
        public ConsultationStatus? ConsultationStatus { get; set; }

        /// <summary>看诊结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// 看诊验证结果
    /// </summary>
    public class ConsultationValidationResult
    {
        /// <summary>是否有效</summary>
        public bool IsValid { get; set; }

        /// <summary>错误消息</summary>
        public List<string> ErrorMessages { get; set; } = new();
    }
}