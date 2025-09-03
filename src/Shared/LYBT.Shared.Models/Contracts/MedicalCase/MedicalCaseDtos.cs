using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医疗案例DTO - UltraThink v2.0简化版
    /// 与MedicalCase实体对齐，保留ConsultationDate
    /// </summary>
    public class MedicalCaseDto : StatusDto, IRemarkable
    {
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        [DisplayName("诊断ID")]
        public Guid? ConsultationId { get; set; }

        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

        [DisplayName("看诊时间")]
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        /// <summary>医疗案例专用状态</summary>
        [DisplayName("案例状态")]
        public MedicalCaseStatus CaseStatus { get; set; } = MedicalCaseStatus.Registered;

        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        /// <summary>获取优先级 - 基于看诊时间</summary>
        public int GetPriority()
        {
            var hoursElapsed = (DateTime.Now - ConsultationDate).TotalHours;
            if (hoursElapsed > 48) return 3; // 高优先级
            if (hoursElapsed > 24) return 2; // 中优先级
            return 1; // 低优先级
        }

        /// <summary>是否紧急</summary>
        public bool IsUrgent() => GetPriority() >= 3;

        /// <summary>是否需要医生注意 - 基于看诊时间</summary>
        public bool NeedsDoctorAttention() => CaseStatus != MedicalCaseStatus.Completed && (DateTime.Now - ConsultationDate).TotalHours > 24;

        /// <summary>是否可以开始诊疗</summary>
        public bool CanStartConsultation() => CaseStatus == MedicalCaseStatus.Registered;

        /// <summary>是否可以完成</summary>
        public bool CanComplete() => CaseStatus == MedicalCaseStatus.InConsultation;

        /// <summary>是否可以取消</summary>
        public bool CanCancel() => CaseStatus == MedicalCaseStatus.Registered || CaseStatus == MedicalCaseStatus.InConsultation;

        /// <summary>是否可以删除</summary>
        public bool CanDelete() => CaseStatus == MedicalCaseStatus.Cancelled || CaseStatus == MedicalCaseStatus.Completed;

        /// <summary>是否可以编辑</summary>
        public bool CanEdit() => CaseStatus != MedicalCaseStatus.Completed && CaseStatus != MedicalCaseStatus.Cancelled;

        /// <summary>是否已完成</summary>
        public bool IsCompleted() => CaseStatus == MedicalCaseStatus.Completed;


    }

    /// <summary>
    /// 医疗案例详情DTO - 继承基础DTO，添加详细信息
    /// </summary>
    public class MedicalCaseDetailDto : MedicalCaseDto
    {
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        [DisplayName("既往史")]
        public string? PastHistory { get; set; }

        [DisplayName("体格检查")]
        public string? PhysicalExamination { get; set; }

        [DisplayName("辅助检查")]
        public string? AuxiliaryExamination { get; set; }

        [DisplayName("诊断结果")]
        public string? DiagnosisResult { get; set; }

        [DisplayName("治疗方案")]
        public string? TreatmentPlan { get; set; }

        [DisplayName("处方信息")]
        public string? PrescriptionInfo { get; set; }

        [DisplayName("随访计划")]
        public string? FollowUpPlan { get; set; }
    }

    /// <summary>
    /// 医疗案例输入基础DTO - 提供通用输入字段验证规则
    /// </summary>
    public abstract class MedicalCaseInputBaseDto : IRemarkable
    {
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建医疗案例DTO - 继承医疗案例输入基础DTO
    /// </summary>
    public class MedicalCaseCreateDto : MedicalCaseInputBaseDto
    {
        [StringLength(200, ErrorMessage = "诊断摘要长度不能超过200个字符")]
        [DisplayName("诊断摘要")]
        public string? DiagnosisSummary { get; set; }
    }

    /// <summary>
    /// 编辑医疗案例DTO - 继承医疗案例输入基础DTO并添加ID字段
    /// </summary>
    public class MedicalCaseEditDto : MedicalCaseInputBaseDto, IIdentifiable<Guid>
    {
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        [DisplayName("医疗案例ID")]
        public Guid Id { get; set; }

        [StringLength(200, ErrorMessage = "诊断摘要长度不能超过200个字符")]
        [DisplayName("诊断摘要")]
        public string? DiagnosisSummary { get; set; }

        [StringLength(1000, ErrorMessage = "主诉长度不能超过1000个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        [StringLength(2000, ErrorMessage = "现病史长度不能超过2000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        [StringLength(1000, ErrorMessage = "既往史长度不能超过1000个字符")]
        [DisplayName("既往史")]
        public string? PastHistory { get; set; }

        [StringLength(1000, ErrorMessage = "诊断结果长度不能超过1000个字符")]
        [DisplayName("诊断结果")]
        public string? DiagnosisResult { get; set; }

        [StringLength(1000, ErrorMessage = "治疗方案长度不能超过1000个字符")]
        [DisplayName("治疗方案")]
        public string? TreatmentPlan { get; set; }

        [DisplayName("状态")]
        public string? Status { get; set; }


    }

    /// <summary>
    /// 更新医疗案例DTO - 继承编辑DTO，用于更复杂的更新操作
    /// </summary>
    public class MedicalCaseUpdateDto : MedicalCaseEditDto
    {

        [StringLength(1000, ErrorMessage = "体格检查长度不能超过1000个字符")]
        [DisplayName("体格检查")]
        public string? PhysicalExamination { get; set; }

        [StringLength(1000, ErrorMessage = "辅助检查长度不能超过1000个字符")]
        [DisplayName("辅助检查")]
        public string? AuxiliaryExamination { get; set; }

        [StringLength(1000, ErrorMessage = "处方信息长度不能超过1000个字符")]
        [DisplayName("处方信息")]
        public string? PrescriptionInfo { get; set; }

        [StringLength(1000, ErrorMessage = "随访计划长度不能超过1000个字符")]
        [DisplayName("随访计划")]
        public string? FollowUpPlan { get; set; }
    }

    /// <summary>
    /// 医疗案例查询DTO - 继承完整分页查询DTO，提供分页、时间范围、关键词搜索功能
    /// </summary>
    public class MedicalCaseQueryDto : ExtendedQueryDto
    {
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        [DisplayName("案例状态")]
        public string? CaseStatus { get; set; }

        [DisplayName("排序字段")]
        public string OrderBy { get; set; } = "CreateTime";

        [DisplayName("升序排序")]
        public bool IsAscending { get; set; } = false;
    }

    /// <summary>
    /// 医疗案例统计DTO - 继承统计DTO基础类
    /// </summary>
    public class MedicalCaseStatisticsDto : StatisticsDto
    {
        [DisplayName("进行中案例数量")]
        public int InProgressCount { get; set; }

        [DisplayName("已完成案例数量")]
        public int CompletedCount { get; set; }

        [DisplayName("已取消案例数量")]
        public int CancelledCount { get; set; }

        [DisplayName("平均完成时间(天)")]
        public double AverageCompletionDays { get; set; }

        [DisplayName("医生案例分布")]
        public Dictionary<string, int> DoctorCaseDistribution { get; set; } = new();

        [DisplayName("月度趋势")]
        public Dictionary<string, int> MonthlyTrend { get; set; } = new();
    }

    /// <summary>
    /// 医案验证结果DTO
    /// </summary>
    public class MedicalCaseValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// 医案统计摘要DTO
    /// </summary>
    public class MedicalCaseStatisticsSummaryDto
    {
        [DisplayName("总医案数")]
        public int TotalMedicalCases { get; set; }

        [DisplayName("已登记数")]
        public int RegisteredCases { get; set; }

        [DisplayName("看诊中数")]
        public int InConsultationCases { get; set; }

        [DisplayName("已完成数")]
        public int CompletedCases { get; set; }

        [DisplayName("已取消数")]
        public int CancelledCases { get; set; }

        [DisplayName("平均看诊时长(分钟)")]
        public double AverageConsultationDuration { get; set; }

        [DisplayName("完成率")]
        public decimal CompletionRate => TotalMedicalCases > 0 ? (decimal)CompletedCases / TotalMedicalCases * 100 : 0;
    }

    /// <summary>
    /// 患者医案统计DTO
    /// </summary>
    public class PatientMedicalCaseStatDto
    {
        public Guid PatientId { get; set; }
        
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("总医案数")]
        public int TotalMedicalCases { get; set; }

        [DisplayName("完成医案数")]
        public int CompletedCases { get; set; }

        [DisplayName("完成率")]
        public decimal CompletionRate { get; set; }

        [DisplayName("首次就诊时间")]
        public DateTime? FirstVisitDate { get; set; }

        [DisplayName("最近就诊时间")]
        public DateTime? LastVisitDate { get; set; }

        [DisplayName("平均就诊间隔(天)")]
        public decimal AverageVisitInterval { get; set; }
    }

    /// <summary>
    /// 医生医案统计DTO
    /// </summary>
    public class DoctorMedicalCaseStatisticsDto
    {
        public Guid DoctorId { get; set; }

        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        [DisplayName("总医案数")]
        public int TotalMedicalCases { get; set; }

        [DisplayName("完成医案数")]
        public int CompletedCases { get; set; }

        [DisplayName("完成率")]
        public decimal CompletionRate { get; set; }

        [DisplayName("平均看诊时长(分钟)")]
        public decimal AverageConsultationTime { get; set; }

        [DisplayName("总患者数")]
        public int TotalPatients { get; set; }
    }

    /// <summary>
    /// 医案批量操作结果DTO
    /// </summary>
    public class MedicalCaseBatchOperationResultDto
    {
        [DisplayName("总数量")]
        public int TotalCount { get; set; }

        [DisplayName("成功数量")]
        public int SuccessCount { get; set; }

        [DisplayName("失败数量")]
        public int FailureCount { get; set; }

        [DisplayName("成功的ID列表")]
        public List<Guid> SuccessfulIds { get; set; } = new();

        [DisplayName("失败的ID列表")]
        public List<Guid> FailedIds { get; set; } = new();

        [DisplayName("错误信息列表")]
        public List<string> ErrorMessages { get; set; } = new();

        [DisplayName("操作成功率")]
        public decimal SuccessRate => TotalCount > 0 ? (decimal)SuccessCount / TotalCount * 100 : 0;
    }

    /// <summary>
    /// 诊疗流程状态DTO
    /// </summary>
    public class ConsultationWorkflowStatusDto
    {
        public Guid MedicalCaseId { get; set; }

        [DisplayName("当前状态")]
        public MedicalCaseStatus CurrentStatus { get; set; }

        [DisplayName("当前步骤")]
        public string CurrentStep { get; set; } = string.Empty;

        [DisplayName("最后更新时间")]
        public DateTime LastUpdatedAt { get; set; }

        public Guid DoctorId { get; set; }

        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        [DisplayName("已完成步骤")]
        public List<string> CompletedSteps { get; set; } = new();

        [DisplayName("待处理步骤")]
        public List<string> PendingSteps { get; set; } = new();

        [DisplayName("可进行下一步")]
        public bool CanProceedToNext { get; set; }
    }

    /// <summary>
    /// 医案高级搜索DTO
    /// </summary>
    public class MedicalCaseAdvancedSearchDto : PagedQueryBaseDto
    {
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        [DisplayName("状态")]
        public MedicalCaseStatus? Status { get; set; }

        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }

        [DisplayName("诊断关键词")]
        public string? DiagnosisKeyword { get; set; }
    }

    /// <summary>
    /// 诊断频次统计DTO
    /// </summary>
    public class DiagnosisFrequencyDto
    {
        [DisplayName("诊断名称")]
        public string DiagnosisName { get; set; } = string.Empty;

        [DisplayName("出现次数")]
        public int Count { get; set; }

        [DisplayName("百分比")]
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// 医案时长分布DTO
    /// </summary>
    public class MedicalCaseDurationDistributionDto
    {
        [DisplayName("平均时长(分钟)")]
        public double AverageMinutes { get; set; }

        [DisplayName("中位数时长(分钟)")]
        public double MedianMinutes { get; set; }

        [DisplayName("最短时长(分钟)")]
        public double MinMinutes { get; set; }

        [DisplayName("最长时长(分钟)")]
        public double MaxMinutes { get; set; }
    }

    /// <summary>
    /// 月度医案趋势DTO
    /// </summary>
    public class MonthlyMedicalCaseTrendDto
    {
        [DisplayName("月份")]
        public string Month { get; set; } = string.Empty;

        [DisplayName("医案数量")]
        public int Count { get; set; }

        [DisplayName("完成数量")]
        public int CompletedCount { get; set; }

        [DisplayName("环比增长率")]
        public decimal GrowthRate { get; set; }
    }

    /// <summary>
    /// 医案高峰时段DTO
    /// </summary>
    public class MedicalCasePeakHourDto
    {
        [DisplayName("小时")]
        public int Hour { get; set; }

        [DisplayName("医案数量")]
        public int Count { get; set; }

        [DisplayName("占比")]
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// 频繁就诊患者DTO
    /// </summary>
    public class FrequentPatientDto
    {
        public Guid PatientId { get; set; }

        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("就诊次数")]
        public int VisitCount { get; set; }

        [DisplayName("天数内")]
        public int DaysWithin { get; set; }

        [DisplayName("最近就诊时间")]
        public DateTime LastVisitDate { get; set; }
    }

    /// <summary>
    /// 医案模式分析DTO
    /// </summary>
    public class MedicalCasePatternDto
    {
        [DisplayName("模式名称")]
        public string PatternName { get; set; } = string.Empty;

        [DisplayName("出现次数")]
        public int OccurrenceCount { get; set; }

        [DisplayName("模式描述")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 患者医案趋势DTO
    /// </summary>
    public class PatientMedicalCaseTrendDto
    {
        [DisplayName("月份")]
        public string Month { get; set; } = string.Empty;

        [DisplayName("医案数量")]
        public int Count { get; set; }

        [DisplayName("趋势方向")]
        public string Trend { get; set; } = string.Empty; // "上升"、"下降"、"稳定"
    }

    /// <summary>
    /// 医案患者信息DTO
    /// </summary>
    public class MedicalCasePatientInfoDto
    {
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }

        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("患者电话")]
        public string? PatientPhone { get; set; }

        [DisplayName("患者年龄")]
        public int? PatientAge { get; set; }
    }

    /// <summary>
    /// 医案看诊信息DTO
    /// </summary>
    public class MedicalCaseConsultationInfoDto
    {
        public Guid MedicalCaseId { get; set; }
        public Guid? ConsultationId { get; set; }

        [DisplayName("看诊状态")]
        public string ConsultationStatus { get; set; } = string.Empty;

        [DisplayName("诊断结果")]
        public string? DiagnosisResult { get; set; }
    }

    /// <summary>
    /// 医案完整信息DTO
    /// </summary>
    public class MedicalCaseCompleteInfoDto : MedicalCaseDetailDto
    {
        [DisplayName("患者完整信息")]
        public string PatientFullInfo { get; set; } = string.Empty;

        [DisplayName("医生完整信息")]
        public string DoctorFullInfo { get; set; } = string.Empty;

        [DisplayName("看诊完整记录")]
        public string ConsultationFullRecord { get; set; } = string.Empty;
    }

    /// <summary>
    /// 医案缓存统计DTO
    /// </summary>
    public class MedicalCaseCacheStatisticsDto
    {
        [DisplayName("缓存项总数")]
        public int TotalCacheItems { get; set; }

        [DisplayName("医案缓存数量")]
        public int MedicalCaseCacheCount { get; set; }

        [DisplayName("患者医案缓存数量")]
        public int PatientMedicalCaseCacheCount { get; set; }

        [DisplayName("医生医案缓存数量")]
        public int DoctorMedicalCaseCacheCount { get; set; }

        [DisplayName("总内存使用(字节)")]
        public long TotalMemoryUsage { get; set; }

        [DisplayName("命中率")]
        public double HitRate { get; set; }

        [DisplayName("最后清理时间")]
        public DateTime LastClearTime { get; set; }
    }

    /// <summary>
    /// 医案查询性能统计DTO
    /// </summary>
    public class MedicalCaseQueryPerformanceStatDto
    {
        [DisplayName("总查询次数")]
        public long TotalQueries { get; set; }

        [DisplayName("平均响应时间(毫秒)")]
        public double AverageResponseTime { get; set; }

        [DisplayName("最慢查询时间(毫秒)")]
        public double SlowestQueryTime { get; set; }

        [DisplayName("最快查询时间(毫秒)")]
        public double FastestQueryTime { get; set; }

        [DisplayName("缓存命中次数")]
        public long CacheHits { get; set; }

        [DisplayName("缓存未命中次数")]
        public long CacheMisses { get; set; }

        [DisplayName("缓存命中率")]
        public double CacheHitRate => (CacheHits + CacheMisses) > 0 ? (double)CacheHits / (CacheHits + CacheMisses) * 100 : 0;
    }
}