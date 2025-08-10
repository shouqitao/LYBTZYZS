using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 看诊信息DTO - 继承基础DTO
    /// 用于看诊列表展示
    /// </summary>
    public class ConsultationDto : BaseDto
    {
        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>用户ID（医生）</summary>
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>诊断</summary>
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>看诊时间</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationTime { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 看诊详情DTO - 继承审计基础DTO + 备注接口
    /// 用于看诊详情的展示和传输
    /// </summary>
    public class ConsultationDetailDto : AuditableDto, IRemarkable
    {
        /// <summary>医疗案例ID</summary>
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>用户ID（兼容旧属性）</summary>
        public Guid UserId => DoctorId;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>看诊时间</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationTime { get; set; }

        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>望诊结果</summary>
        [StringLength(500, ErrorMessage = "望诊结果长度不能超过500个字符")]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊结果</summary>
        [StringLength(500, ErrorMessage = "闻诊结果长度不能超过500个字符")]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊结果</summary>
        [StringLength(500, ErrorMessage = "问诊结果长度不能超过500个字符")]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊结果</summary>
        [StringLength(500, ErrorMessage = "切诊结果长度不能超过500个字符")]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>舌诊结果</summary>
        [StringLength(500, ErrorMessage = "舌诊结果长度不能超过500个字符")]
        [DisplayName("舌诊")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊结果</summary>
        [StringLength(500, ErrorMessage = "脉诊结果长度不能超过500个字符")]
        [DisplayName("脉诊")]
        public string? PulseCondition { get; set; }

        /// <summary>辨证分析</summary>
        [StringLength(800, ErrorMessage = "辨证分析长度不能超过800个字符")]
        [DisplayName("辨证分析")]
        public string? PatternDifferentiation { get; set; }

        /// <summary>中医辨证</summary>
        [StringLength(500, ErrorMessage = "中医辨证长度不能超过500个字符")]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>诊断结果</summary>
        [Required(ErrorMessage = "诊断结果不能为空")]
        [StringLength(500, ErrorMessage = "诊断结果长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>治疗原则</summary>
        [StringLength(500, ErrorMessage = "治疗原则长度不能超过500个字符")]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(1000, ErrorMessage = "医嘱长度不能超过1000个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>看诊开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>看诊结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>看诊状态</summary>
        [DisplayName("状态")]
        public ConsultationStatus Status { get; set; } = ConsultationStatus.InProgress;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 看诊创建DTO - 前后端共享API契约
    /// 用于创建新看诊记录的请求模型
    /// </summary>
    public class ConsultationCreateDto
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

        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>看诊开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 看诊更新DTO - 继承基础DTO + 备注接口
    /// 用于更新看诊记录的请求模型
    /// </summary>
    public class ConsultationUpdateDto : BaseDto, IRemarkable
    {
        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>望诊结果</summary>
        [StringLength(500, ErrorMessage = "望诊结果长度不能超过500个字符")]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊结果</summary>
        [StringLength(500, ErrorMessage = "闻诊结果长度不能超过500个字符")]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊结果</summary>
        [StringLength(500, ErrorMessage = "问诊结果长度不能超过500个字符")]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊结果</summary>
        [StringLength(500, ErrorMessage = "切诊结果长度不能超过500个字符")]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>舌诊结果</summary>
        [StringLength(500, ErrorMessage = "舌诊结果长度不能超过500个字符")]
        [DisplayName("舌诊")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊结果</summary>
        [StringLength(500, ErrorMessage = "脉诊结果长度不能超过500个字符")]
        [DisplayName("脉诊")]
        public string? PulseCondition { get; set; }

        /// <summary>辨证分析</summary>
        [StringLength(800, ErrorMessage = "辨证分析长度不能超过800个字符")]
        [DisplayName("辨证分析")]
        public string? PatternDifferentiation { get; set; }

        /// <summary>中医辨证</summary>
        [StringLength(500, ErrorMessage = "中医辨证长度不能超过500个字符")]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>诊断结果</summary>
        [StringLength(500, ErrorMessage = "诊断结果长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(500, ErrorMessage = "治疗原则长度不能超过500个字符")]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(1000, ErrorMessage = "医嘱长度不能超过1000个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}