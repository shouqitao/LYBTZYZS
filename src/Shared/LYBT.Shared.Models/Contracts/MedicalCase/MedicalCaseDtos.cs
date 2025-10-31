using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{

    /// <summary>
    /// 医疗案例DTO - UltraThink v2.0简化版
    /// 与MedicalCase实体对齐，保留ConsultationDate
    /// </summary>
    public class MedicalCaseDto : StatusDto, IRemarkable
    {
        [DisplayName("案例编号")]
        [StringLength(50, ErrorMessage = "案例编号长度不能超过50个字符")]
        public string? CaseNumber { get; set; }

        [DisplayName("主诉")]
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        public string? ChiefComplaint { get; set; }

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

        /// <summary>获取优先级 - 基于诊疗时间</summary>
        public int GetPriority()
        {
            var hoursElapsed = (DateTime.Now - ConsultationDate).TotalHours;
            if (hoursElapsed > 48)
            {
                return 3; // 高优先级
            }

            if (hoursElapsed > 24)
            {
                return 2; // 中优先级
            }

            return 1; // 低优先级
        }

        /// <summary>是否紧急</summary>
        public bool IsUrgent() => GetPriority() >= 3;

        /// <summary>是否需要医生注意 - 基于诊疗时间</summary>
        public bool NeedsDoctorAttention() =>
            (CaseStatus == MedicalCaseStatus.Active || CaseStatus == MedicalCaseStatus.Draft) &&
            (DateTime.Now - ConsultationDate).TotalHours > 24;

        /// <summary>是否可以开始诊疗</summary>
        public bool CanStartConsultation() => CaseStatus == MedicalCaseStatus.Active;

        /// <summary>是否可以完成</summary>
        public bool CanComplete() => CaseStatus == MedicalCaseStatus.Active;

        /// <summary>是否可以取消</summary>
        public bool CanCancel() => CaseStatus == MedicalCaseStatus.Active;

        /// <summary>是否可以删除 - Epic #1612修正版</summary>
        public bool CanDelete() => CaseStatus == MedicalCaseStatus.Completed || CaseStatus == MedicalCaseStatus.Cancelled;

        /// <summary>是否可以编辑 - Epic #1612修正版</summary>
        public bool CanEdit() => CaseStatus == MedicalCaseStatus.Active || CaseStatus == MedicalCaseStatus.Draft;

        /// <summary>是否已完成 - Epic #1612修正版</summary>
        public bool IsCompleted() => CaseStatus == MedicalCaseStatus.Completed;
    }

    /// <summary>
    /// 医疗案例详情DTO - 继承基础DTO，添加详细信息
    /// Epic #1583 Phase 3: 添加Consultation和Prescription关联数据
    /// </summary>
    public class MedicalCaseDetailDto : MedicalCaseDto
    {

        [DisplayName("主诉")]
        public new string? ChiefComplaint { get; set; }

        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        [DisplayName("诊断结果")]
        public string? DiagnosisResult { get; set; }

        [DisplayName("治疗方案")]
        public string? TreatmentPlan { get; set; }

        /// <summary>
        /// 诊疗记录（Epic #1583 Phase 3: 继续看诊时加载）
        /// </summary>
        [DisplayName("诊疗记录")]
        public ConsultationDto? Consultation { get; set; }

        /// <summary>
        /// 处方信息（Epic #1583 Phase 3: 继续看诊时加载）
        /// </summary>
        [DisplayName("处方信息")]
        public PrescriptionDto? Prescription { get; set; }
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

        /// <inheritdoc/>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建医疗案例DTO - 继承医疗案例输入基础DTO
    /// </summary>
    public class MedicalCaseCreateDto : MedicalCaseInputBaseDto
    {
        [DisplayName("案例编号")]
        [StringLength(50, ErrorMessage = "案例编号长度不能超过50个字符")]
        public string? CaseNumber { get; set; }

        [DisplayName("主诉")]
        [StringLength(1000, ErrorMessage = "主诉长度不能超过1000个字符")]
        public string? ChiefComplaint { get; set; }

        [DisplayName("现病史")]
        [StringLength(2000, ErrorMessage = "现病史长度不能超过2000个字符")]
        public string? PresentIllnessHistory { get; set; }

        [DisplayName("既往史")]
        [StringLength(1000, ErrorMessage = "既往史长度不能超过1000个字符")]
        public string? PastMedicalHistory { get; set; }

        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Active;

        [StringLength(200, ErrorMessage = "诊断摘要长度不能超过200个字符")]
        [DisplayName("诊断摘要")]
        public string? DiagnosisSummary { get; set; }
    }

    /// <summary>
    /// 编辑医疗案例DTO - 继承医疗案例输入基础DTO并添加ID字段
    /// </summary>
    public class MedicalCaseEditDto : MedicalCaseInputBaseDto, IIdentifiable<Guid>
    {

        /// <inheritdoc/>
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
        /// <summary>诊断ID - Issue #1544: 支持更新ConsultationId</summary>
        [DisplayName("诊断ID")]
        public Guid? ConsultationId { get; set; }

        /// <summary>处方ID - Issue #1545: 支持更新PrescriptionId</summary>
        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

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
    /// 医疗案例查询DTO - 基础查询条件
    /// </summary>
    public class MedicalCaseQueryDto : PagedQueryBaseDto
    {
        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        /// <summary>案例状态</summary>
        [DisplayName("案例状态")]
        public MedicalCaseStatus? CaseStatus { get; set; }

        /// <summary>关键词搜索</summary>
        [DisplayName("关键词")]
        public new string? Keyword { get; set; }
    }

    /// <summary>
    /// 医疗案例搜索DTO - 高级搜索条件
    /// </summary>
    public class MedicalCaseSearchDto : MedicalCaseQueryDto
    {
        /// <summary>诊断关键词</summary>
        [DisplayName("诊断关键词")]
        public string? DiagnosisKeyword { get; set; }

        /// <summary>开始日期</summary>
        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        /// <summary>结束日期</summary>
        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }

        /// <summary>主诉关键词</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        /// <summary>是否包含已关闭案例</summary>
        [DisplayName("包含已关闭")]
        public bool IncludeClosed { get; set; } = false;

        /// <summary>排序字段</summary>
        [DisplayName("排序字段")]
        public string OrderBy { get; set; } = "CreateTime";

        /// <summary>升序排序</summary>
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
    /// 医案验证结果DTO - 继承自通用验证结果基类
    /// </summary>
    public class MedicalCaseValidationResult : ValidationResultDto
    {
        // 继承所有基类字段，无需额外定义
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

        [DisplayName("诊疗中数")]
        public int InConsultationCases { get; set; }

        [DisplayName("已完成数")]
        public int CompletedCases { get; set; }

        [DisplayName("已取消数")]
        public int CancelledCases { get; set; }

        [DisplayName("平均诊疗时长(分钟)")]
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

        [DisplayName("平均诊疗时长(分钟)")]
        public decimal AverageConsultationTime { get; set; }

        [DisplayName("总患者数")]
        public int TotalPatients { get; set; }
    }

    /// <summary>
    /// 医案批量操作结果DTO - 继承自通用批量操作结果基类
    /// </summary>
    public class MedicalCaseBatchOperationResultDto : BatchOperationResultDto
    {
        // 继承所有基类字段，无需额外定义
    }

    /// <summary>
    /// 诊疗流程状态DTO (Record-Only模式：仅数据记录，无复杂流程控制)
    /// </summary>
    public class ConsultationProcessStatusDto
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
    /// 医案完整聚合创建DTO - 包含诊疗和可选处方
    /// 作为聚合根统一管理整个诊疗流程
    /// </summary>
    public class MedicalCaseWithDetailsCreateDto
    {
        /// <summary>医案基础信息</summary>
        [Required(ErrorMessage = "医案信息不能为空")]
        [DisplayName("医案信息")]
        public MedicalCaseCreateDto MedicalCase { get; set; } = new();

        /// <summary>诊疗记录信息（必需）</summary>
        [Required(ErrorMessage = "诊疗信息不能为空")]
        [DisplayName("诊疗信息")]
        public ConsultationCreateDto Consultation { get; set; } = new();

        /// <summary>处方信息（可选）</summary>
        [DisplayName("处方信息")]
        public PrescriptionCreateDto? Prescription { get; set; }
    }

    /// <summary>
    /// 医案+处方联建创建DTO - Phase B2 事务优化
    /// 在单个事务中创建医案和关联处方
    /// </summary>
    public class MedicalCaseWithPrescriptionCreateDto
    {
        /// <summary>医案创建信息</summary>
        [Required(ErrorMessage = "医案信息不能为空")]
        [DisplayName("医案信息")]
        public MedicalCaseCreateDto MedicalCase { get; set; } = new();

        /// <summary>处方创建信息（可选）</summary>
        [DisplayName("处方信息")]
        public PrescriptionCreateDto? Prescription { get; set; }

        /// <summary>是否立即创建处方</summary>
        [DisplayName("立即创建处方")]
        public bool CreatePrescriptionImmediately { get; set; } = false;
    }

    /// <summary>
    /// 医案+处方联建结果DTO - Phase B2 事务优化
    /// </summary>
    public class MedicalCaseWithPrescriptionResultDto
    {
        /// <summary>创建的医案信息</summary>
        [DisplayName("医案信息")]
        public MedicalCaseDto MedicalCase { get; set; } = new();

        /// <summary>创建的处方信息（如果创建了处方）</summary>
        [DisplayName("处方信息")]
        public PrescriptionDto? Prescription { get; set; }

        /// <summary>操作是否成功</summary>
        [DisplayName("操作成功")]
        public bool IsSuccess { get; set; } = true;

        /// <summary>操作消息</summary>
        [DisplayName("操作消息")]
        public string Message { get; set; } = string.Empty;
    }

    // ========== 医案状态操作DTO（从Controller迁移到Shared） ==========

    /// <summary>
    /// 完成医案DTO
    /// </summary>
    public class CompleteMedicalCaseDto
    {
        [StringLength(500, ErrorMessage = "完成原因长度不能超过500个字符")]
        [DisplayName("完成原因")]
        public string? CompletionReason { get; set; }
    }

    /// <summary>
    /// 暂停医案DTO
    /// </summary>
    public class SuspendMedicalCaseDto
    {
        [StringLength(500, ErrorMessage = "暂停原因长度不能超过500个字符")]
        [DisplayName("暂停原因")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 更新医案状态DTO
    /// </summary>
    public class UpdateMedicalCaseStatusDto
    {
        [Required(ErrorMessage = "状态不能为空")]
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "状态变更原因长度不能超过500个字符")]
        [DisplayName("状态变更原因")]
        public string? StatusChangeReason { get; set; }
    }

    /// <summary>
    /// 归档医案DTO
    /// </summary>
    public class ArchiveMedicalCaseDto
    {
        [StringLength(500, ErrorMessage = "归档原因长度不能超过500个字符")]
        [DisplayName("归档原因")]
        public string? ArchiveReason { get; set; }
    }

    // ========== Epic #1583: 待看诊队列DTO ==========

    /// <summary>
    /// 待看诊队列项DTO
    /// Epic #1583 - Phase 5: Server端API
    /// 用于患者选择界面的待看诊队列显示
    /// </summary>
    public class PendingMedicalCaseDto
    {
        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>手机号（原始）</summary>
        [DisplayName("手机号")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>手机号（脱敏）</summary>
        [DisplayName("手机号脱敏")]
        public string PhoneMasked { get; set; } = string.Empty;

        /// <summary>类型（"暂存" 或 "已挂号"）</summary>
        [DisplayName("类型")]
        public string Type { get; set; } = string.Empty;

        /// <summary>医案ID（如果有未完成医案，则有值；挂号患者为null）</summary>
        [DisplayName("医案ID")]
        public Guid? MedicalCaseId { get; set; }
    }

    /// <summary>
    /// 标记是否开处方请求
    /// Task 3.4 (#1661): RadioBox变化时自动保存
    /// </summary>
    public class SetPrescriptionFlagRequest
    {
        /// <summary>是否需要开处方</summary>
        [Required(ErrorMessage = "开处方标志不能为空")]
        public bool NeedsPrescription { get; set; }
    }
}
