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
    /// 注：MedicalCase使用CaseStatus管理业务状态，不再使用CommonStatus
    /// </summary>
    public class MedicalCaseDto : TimestampDto, IRemarkable
    {
        [DisplayName("案例编号")]
        [StringLength(50, ErrorMessage = "案例编号长度不能超过50个字符")]
        public string? CaseNumber { get; set; }

        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint，改用Consultation.PresentIllness

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
    }

    /// <summary>
    /// 医疗案例详情DTO - 继承基础DTO，添加详细信息
    /// Epic #1583 Phase 3: 添加Consultation和Prescription关联数据
    /// </summary>
    public class MedicalCaseDetailDto : MedicalCaseDto
    {
        // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段，通过Consultation获取
        // 移除：ChiefComplaint, DiagnosisResult, TreatmentPlan
        // 保留：PresentIllness (映射自Consultation.PresentIllness)

        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

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

        // OpenSpec: refactor-diagnosis-fields - 诊断字段精简到Consultation
        // 移除：ChiefComplaint, PastHistory, DiagnosisResult, TreatmentPlan

        [StringLength(2000, ErrorMessage = "现病史长度不能超过2000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        [DisplayName("状态")]
        public string? Status { get; set; }
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

        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint搜索字段

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
    /// Epic #1961: 使用统一的 MedicalCaseInputDto
    /// </summary>
    public class MedicalCaseWithDetailsCreateDto
    {
        /// <summary>医案基础信息</summary>
        [Required(ErrorMessage = "医案信息不能为空")]
        [DisplayName("医案信息")]
        public MedicalCaseInputDto MedicalCase { get; set; } = new();

        /// <summary>诊疗记录信息（必需）</summary>
        [Required(ErrorMessage = "诊疗信息不能为空")]
        [DisplayName("诊疗信息")]
        public ConsultationInputDto Consultation { get; set; } = new();

        /// <summary>处方信息（可选）</summary>
        [DisplayName("处方信息")]
        public PrescriptionCreateDto? Prescription { get; set; }
    }

    /// <summary>
    /// 医案+处方联建创建DTO - Phase B2 事务优化
    /// 在单个事务中创建医案和关联处方
    /// Epic #1961: 使用统一的 MedicalCaseInputDto
    /// </summary>
    public class MedicalCaseWithPrescriptionCreateDto
    {
        /// <summary>医案创建信息</summary>
        [Required(ErrorMessage = "医案信息不能为空")]
        [DisplayName("医案信息")]
        public MedicalCaseInputDto MedicalCase { get; set; } = new();

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

    // ========== 医案状态操作DTO ==========

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

    /// <summary>
    /// 医案权限详情DTO
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
    /// 用于前后端传递用户对医案的权限信息
    /// </summary>
    public class MedicalCasePermissionDto
    {
        /// <summary>是否可编辑</summary>
        [DisplayName("可编辑")]
        public bool CanEdit { get; set; }

        /// <summary>是否可删除</summary>
        [DisplayName("可删除")]
        public bool CanDelete { get; set; }

        /// <summary>是否需要修改原因（编辑已完成医案时需要）</summary>
        [DisplayName("需要修改原因")]
        public bool RequiresEditReason { get; set; }

        /// <summary>是否只读模式</summary>
        [DisplayName("只读")]
        public bool IsReadOnly => !CanEdit;

        /// <summary>权限拒绝原因（如果无权限）</summary>
        [DisplayName("拒绝原因")]
        public string? DenialReason { get; set; }
    }

    /// <summary>
    /// 医案审计日志DTO
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// 用于前后端传递医案的修改历史记录
    /// </summary>
    public class MedicalCaseAuditLogDto
    {
        /// <summary>唯一标识</summary>
        [DisplayName("唯一标识")]
        public Guid Id { get; set; }

        /// <summary>医案ID</summary>
        [DisplayName("医案ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>操作者ID</summary>
        [DisplayName("操作者ID")]
        public Guid OperatorId { get; set; }

        /// <summary>操作者姓名</summary>
        [DisplayName("操作者姓名")]
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>操作者角色</summary>
        [DisplayName("操作者角色")]
        public UserRole OperatorRole { get; set; }

        /// <summary>操作类型</summary>
        [DisplayName("操作类型")]
        public AuditOperationType OperationType { get; set; }

        /// <summary>操作类型显示名称</summary>
        [DisplayName("操作类型名称")]
        public string OperationTypeName => OperationType switch
        {
            AuditOperationType.Create => "创建",
            AuditOperationType.Update => "更新",
            AuditOperationType.StatusChange => "状态变更",
            AuditOperationType.SoftDelete => "软删除",
            _ => "未知"
        };

        /// <summary>变更的字段列表（JSON格式）</summary>
        [DisplayName("变更字段")]
        public string? ChangedFields { get; set; }

        /// <summary>变更前的值（JSON格式）</summary>
        [DisplayName("原值")]
        public string? OldValues { get; set; }

        /// <summary>变更后的值（JSON格式）</summary>
        [DisplayName("新值")]
        public string? NewValues { get; set; }

        /// <summary>修改原因（历史医案修改时必填）</summary>
        [DisplayName("修改原因")]
        public string? Reason { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 医案审计日志分页结果DTO
    /// </summary>
    public class MedicalCaseAuditLogPagedResultDto
    {
        /// <summary>审计日志列表</summary>
        [DisplayName("日志列表")]
        public List<MedicalCaseAuditLogDto> Logs { get; set; } = new();

        /// <summary>总记录数</summary>
        [DisplayName("总记录数")]
        public int TotalCount { get; set; }

        /// <summary>当前页码</summary>
        [DisplayName("当前页")]
        public int CurrentPage { get; set; }

        /// <summary>每页大小</summary>
        [DisplayName("每页大小")]
        public int PageSize { get; set; }

        /// <summary>总页数</summary>
        [DisplayName("总页数")]
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }

    #region UpdateMedicalCaseRequest (从LYBT.Module.MedicalCase.Dtos迁移)

    /// <summary>
    /// MedicalCase统一更新请求模型
    /// Epic #1612: MedicalCase模块权限优化 - Phase 2 Task 2.3
    /// 合并6个分散的更新方法为统一的更新接口
    /// OpenSpec: consolidate-medicalcase-dtos - 从Server模块层迁移到Shared层
    /// </summary>
    public class UpdateMedicalCaseRequest
    {
        #region 基本属性

        /// <summary>
        /// 病案状态
        /// </summary>
        public MedicalCaseStatus? Status { get; set; }

        /// <summary>
        /// 是否需要处方（三步流程Step 2）
        /// </summary>
        public bool? NeedsPrescription { get; set; }

        #endregion

        #region 辨证信息（Step 1）

        /// <summary>
        /// 辨证信息更新（三步流程Step 1）
        /// 如果提供，则更新Consultation实体
        /// </summary>
        public ConsultationInputDto? Consultation { get; set; }

        #endregion

        #region 处方操作（Step 3）

        /// <summary>
        /// 创建处方请求（三步流程Step 3a）
        /// </summary>
        public PrescriptionCreateDto? CreatePrescription { get; set; }

        /// <summary>
        /// 更新处方请求（三步流程Step 3b）
        /// </summary>
        public MedicalCasePrescriptionUpdateRequest? UpdatePrescription { get; set; }

        /// <summary>
        /// 删除处方请求
        /// </summary>
        public MedicalCaseDeletePrescriptionRequest? DeletePrescription { get; set; }

        /// <summary>
        /// 完成病案请求（三步流程完成）
        /// </summary>
        public MedicalCaseCompleteCaseRequest? CompleteCase { get; set; }

        #endregion

        #region 操作模式选项

        /// <summary>
        /// 更新模式
        /// </summary>
        public MedicalCaseUpdateMode Mode { get; set; } = MedicalCaseUpdateMode.UpdateAll;

        /// <summary>
        /// 是否跳过业务规则验证（仅管理员可用）
        /// </summary>
        public bool SkipBusinessRules { get; set; } = false;

        /// <summary>
        /// 是否强制执行（覆盖状态检查）
        /// </summary>
        public bool Force { get; set; } = false;

        #endregion
    }

    /// <summary>
    /// 更新模式枚举
    /// OpenSpec: consolidate-medicalcase-dtos - 从Server模块层迁移到Shared层
    /// </summary>
    public enum MedicalCaseUpdateMode
    {
        /// <summary>
        /// 更新所有提供的字段
        /// </summary>
        UpdateAll,

        /// <summary>
        /// 仅更新提供的字段，其他保持不变
        /// </summary>
        UpdateOnly,

        /// <summary>
        /// 仅验证，不执行更新
        /// </summary>
        ValidateOnly,

        /// <summary>
        /// 事务模式：要么全部成功，要么全部回滚
        /// </summary>
        Transactional
    }

    /// <summary>
    /// 处方更新请求
    /// OpenSpec: consolidate-medicalcase-dtos - 从Server模块层迁移到Shared层
    /// </summary>
    public class MedicalCasePrescriptionUpdateRequest
    {
        /// <summary>处方ID</summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>处方数据</summary>
        public PrescriptionEditDto PrescriptionData { get; set; } = new();
    }

    /// <summary>
    /// 删除处方请求
    /// OpenSpec: consolidate-medicalcase-dtos - 从Server模块层迁移到Shared层
    /// </summary>
    public class MedicalCaseDeletePrescriptionRequest
    {
        /// <summary>处方ID</summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>删除原因</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 完成病案请求
    /// OpenSpec: consolidate-medicalcase-dtos - 从Server模块层迁移到Shared层
    /// </summary>
    public class MedicalCaseCompleteCaseRequest
    {
        /// <summary>是否跳过三步验证</summary>
        public bool SkipThreeStepValidation { get; set; } = false;

        /// <summary>完成备注</summary>
        public string CompletionNote { get; set; } = string.Empty;
    }

    #endregion
}
