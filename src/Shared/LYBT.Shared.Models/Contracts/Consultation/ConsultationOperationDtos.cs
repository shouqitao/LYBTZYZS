using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Consultation
{

    /// <summary>
    /// 开始看诊DTO - 前后端共享API契约
    /// 用于开始看诊的请求模型
    /// </summary>
    public class ConsultationStartDto
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

        /// <summary>用户ID（兼容旧属性）</summary>
        public Guid UserId => DoctorId;

        /// <summary>预计看诊时长（分钟）</summary>
        [Range(5, 480, ErrorMessage = "预计看诊时长必须在5-480分钟之间")]
        [DisplayName("预计时长")]
        public int EstimatedDuration { get; set; } = 30;

        /// <summary>看诊类型</summary>
        [DisplayName("看诊类型")]
        public string? ConsultationType { get; set; }

        /// <summary>初步主诉</summary>
        [StringLength(500, ErrorMessage = "初步主诉长度不能超过500个字符")]
        [DisplayName("初步主诉")]
        public string? InitialComplaint { get; set; }

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 完成看诊DTO - 前后端共享API契约
    /// 用于完成看诊的请求模型
    /// </summary>
    public class ConsultationCompleteDto
    {

        /// <summary>看诊ID</summary>
        [Required(ErrorMessage = "看诊ID不能为空")]
        [DisplayName("看诊ID")]
        public Guid Id { get; set; }

        /// <summary>最终诊断</summary>
        [Required(ErrorMessage = "诊断结果不能为空")]
        [StringLength(500, ErrorMessage = "诊断结果长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>中医辨证</summary>
        [StringLength(500, ErrorMessage = "中医辨证长度不能超过500个字符")]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(500, ErrorMessage = "治疗原则长度不能超过500个字符")]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医噢</summary>
        [StringLength(1000, ErrorMessage = "医噢长度不能超过1000个字符")]
        [DisplayName("医噢")]
        public string? MedicalAdvice { get; set; }

        /// <summary>治疗建议</summary>
        [StringLength(1000, ErrorMessage = "治疗建议长度不能超过1000个字符")]
        [DisplayName("治疗建议")]
        public string? TreatmentAdvice { get; set; }

        /// <summary>复诊建议</summary>
        [StringLength(500, ErrorMessage = "复诊建议长度不能超过500个字符")]
        [DisplayName("复诊建议")]
        public string? FollowUpAdvice { get; set; }

        /// <summary>预计复诊日期</summary>
        [DisplayName("预计复诊日期")]
        public DateTime? NextVisitDate { get; set; }

        /// <summary>看诊总结</summary>
        [StringLength(1000, ErrorMessage = "看诊总结长度不能超过1000个字符")]
        [DisplayName("看诊总结")]
        public string? Summary { get; set; }

        /// <summary>完成时间</summary>
        [DisplayName("完成时间")]
        public DateTime CompleteTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 看诊分页查询DTO - 继承完整查询基类
    /// 用于看诊记录的分页查询和筛选
    /// </summary>
    public class ConsultationPagedQueryDto : ExtendedQueryDto
    {

        /// <summary>患者姓名关键词</summary>
        [DisplayName("患者姓名")]
        public string? PatientName { get; set; }

        /// <summary>医生姓名关键词</summary>
        [DisplayName("医生姓名")]
        public string? DoctorName { get; set; }

        /// <summary>患者ID筛选</summary>
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        /// <summary>医生ID筛选</summary>
        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        /// <summary>看诊状态筛选</summary>
        [DisplayName("看诊状态")]
        public ConsultationStatus? ConsultationStatus { get; set; }

        /// <summary>诊断关键词</summary>
        [DisplayName("诊断关键词")]
        public string? Diagnosis { get; set; }

        /// <summary>看诊类型筛选</summary>
        [DisplayName("看诊类型")]
        public string? ConsultationType { get; set; }

        /// <summary>是否包含已完成的看诊</summary>
        [DisplayName("包含已完成")]
        public bool IncludeCompleted { get; set; } = true;

        /// <summary>是否包含取消的看诊</summary>
        [DisplayName("包含已取消")]
        public bool IncludeCancelled { get; set; } = false;
    }

    /// <summary>
    /// 看诊状态更新DTO - 前后端共享API契约
    /// 用于更新看诊状态的请求模型
    /// </summary>
    public class UpdateStatusDto
    {

        /// <summary>看诊ID</summary>
        [Required(ErrorMessage = "看诊ID不能为空")]
        [DisplayName("看诊ID")]
        public Guid Id { get; set; }

        /// <summary>新状态</summary>
        [Required(ErrorMessage = "状态不能为空")]
        [DisplayName("状态")]
        public ConsultationStatus Status { get; set; }

        /// <summary>状态变更原因</summary>
        [StringLength(500, ErrorMessage = "变更原因长度不能超过500个字符")]
        [DisplayName("变更原因")]
        public string? Reason { get; set; }

        /// <summary>操作者备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>变更时间</summary>
        [DisplayName("变更时间")]
        public DateTime ChangeTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 看诊统计DTO
    /// </summary>
    public class ConsultationStatisticsDto
    {

        /// <summary>总看诊次数</summary>
        public int TotalConsultations { get; set; }

        /// <summary>进行中的看诊</summary>
        public int InProgressConsultations { get; set; }

        /// <summary>已完成的看诊</summary>
        public int CompletedConsultations { get; set; }

        /// <summary>已取消的看诊</summary>
        public int CancelledConsultations { get; set; }

        /// <summary>今日看诊次数</summary>
        public int TodayConsultations { get; set; }

        /// <summary>平均看诊时长（分钟）</summary>
        public double AverageDuration { get; set; }

        /// <summary>最常见诊断TOP5</summary>
        public Dictionary<string, int> TopDiagnoses { get; set; } = new();

        /// <summary>医生工作量统计</summary>
        public Dictionary<string, int> DoctorWorkload { get; set; } = new();

        /// <summary>统计时间</summary>
        public DateTime StatisticsTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 看诊日程DTO
    /// </summary>
    public class ConsultationScheduleDto
    {

        /// <summary>日期</summary>
        public DateTime Date { get; set; }

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>时段安排</summary>
        public List<TimeSlotDto> TimeSlots { get; set; } = new();

        /// <summary>当日总预约数</summary>
        public int TotalAppointments { get; set; }

        /// <summary>已完成看诊数</summary>
        public int CompletedCount { get; set; }

        /// <summary>取消看诊数</summary>
        public int CancelledCount { get; set; }
    }

    /// <summary>
    /// 时段DTO
    /// </summary>
    public class TimeSlotDto
    {

        /// <summary>开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>结束时间</summary>
        public DateTime EndTime { get; set; }

        /// <summary>患者姓名</summary>
        public string? PatientName { get; set; }

        /// <summary>看诊状态</summary>
        public ConsultationStatus Status { get; set; }

        /// <summary>是否可用</summary>
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// 看诊历史查询DTO
    /// </summary>
    public class ConsultationHistoryQueryDto
    {

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        public Guid PatientId { get; set; }

        /// <summary>查询开始日期</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>查询结束日期</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>医生ID筛选</summary>
        public Guid? DoctorId { get; set; }

        /// <summary>是否包含详细信息</summary>
        public bool IncludeDetails { get; set; } = false;

        /// <summary>最大返回数量</summary>
        [Range(1, 100)]
        public int MaxResults { get; set; } = 20;
    }

    /// <summary>
    /// 取消看诊DTO
    /// </summary>
    public class CancelConsultationDto
    {
        /// <summary>取消原因</summary>
        [StringLength(500, ErrorMessage = "取消原因长度不能超过500个字符")]
        [DisplayName("取消原因")]
        public string? Reason { get; set; }
    }
}
