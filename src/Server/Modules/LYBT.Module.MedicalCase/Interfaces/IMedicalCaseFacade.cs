using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Interfaces;

/// <summary>
/// MedicalCase 门面服务 - 聚合 5 个 CQRS 服务
/// 降低 Controller 依赖数量 (8 -> 3)
/// </summary>
public interface IMedicalCaseFacade
{
    // ===== 写操作 (CommandService) =====

    /// <summary>
    /// 统一保存医案（支持创建和更新）
    /// </summary>
    Task<MedicalCase?> SaveAsync(MedicalCaseInputDto input, Guid operatorId, bool isAdmin);

    /// <summary>
    /// 标记是否需要开处方
    /// </summary>
    Task<MedicalCase?> SetPrescriptionFlagAsync(Guid id, bool flag, Guid operatorId, bool isAdmin);

    /// <summary>
    /// 删除医案（软删除）
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin);

    /// <summary>
    /// 批量删除医案
    /// </summary>
    Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId, bool isAdmin);

    // ===== 状态操作 (StateService) =====

    /// <summary>
    /// 更新医案状态
    /// </summary>
    Task<MedicalCase?> UpdateStatusAsync(Guid id, MedicalCaseStatus status);

    /// <summary>
    /// 统一完成医案入口
    /// </summary>
    Task<MedicalCase?> CompleteAsync(Guid id, Guid operatorId, bool isAdmin, bool skipWorkflowValidation = false);

    /// <summary>
    /// 暂存医案（保存草稿）
    /// </summary>
    Task<MedicalCase?> SaveDraftAsync(Guid id, ConsultationInputDto? input, Guid operatorId, bool isAdmin);

    /// <summary>
    /// 取消医案
    /// </summary>
    Task<MedicalCase?> CancelAsync(Guid id, Guid operatorId, bool isAdmin, string? reason = null);

    // ===== 读操作 (QueryService) =====

    /// <summary>
    /// 根据ID获取医案详情
    /// </summary>
    Task<MedicalCase?> GetByIdAsync(Guid id);

    /// <summary>
    /// 查询医案列表（分页，返回MedicalCaseListDto）
    /// </summary>
    Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync(
        MedicalCaseStatus? status, Guid? patientId, int page, int pageSize,
        Guid? currentDoctorId = null, bool isAdmin = false, string? keyword = null);

    /// <summary>
    /// 查询辨证记录列表
    /// </summary>
    Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId);

    /// <summary>
    /// 查询处方列表
    /// </summary>
    Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId);

    /// <summary>
    /// 获取待看诊队列（医生维度）
    /// </summary>
    Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null);

    /// <summary>
    /// 获取所有待看诊队列（管理员专用）
    /// </summary>
    Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync();

    /// <summary>
    /// 跨医案搜索（支持多条件组合查询）
    /// </summary>
    Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(
        string? patientName = null, string? diagnosisKeyword = null,
        DateTime? startDate = null, DateTime? endDate = null,
        int page = 1, int pageSize = 20);

    /// <summary>
    /// 统一查询接口
    /// </summary>
    Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query);

    /// <summary>
    /// 批量获取医案详情
    /// </summary>
    Task<List<MedicalCase>> GetBatchAsync(List<Guid> ids);

    // ===== 权限/审计 =====

    /// <summary>
    /// 获取用户对医案的权限详情
    /// </summary>
    MedicalCasePermissionDto GetPermissions(Guid userId, UserRole role, MedicalCase mc);

    /// <summary>
    /// 获取医案的审计日志列表（分页）
    /// </summary>
    Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetAuditLogsPagedAsync(
        Guid medicalCaseId, int page = 1, int pageSize = 20);
}
