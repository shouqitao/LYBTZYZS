using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Services;

/// <summary>
/// MedicalCase 门面服务实现 - 聚合 5 个 CQRS 服务的简单委托
/// 降低 Controller 构造函数依赖数量 (8 -> 3)
/// </summary>
public class MedicalCaseFacade : IMedicalCaseFacade
{
    private readonly IMedicalCaseCommandService _commandService;
    private readonly IMedicalCaseQueryService _queryService;
    private readonly IMedicalCaseStateService _stateService;
    private readonly IMedicalCasePermissionService _permissionService;
    private readonly IMedicalCaseAuditService _auditService;

    public MedicalCaseFacade(
        IMedicalCaseCommandService commandService,
        IMedicalCaseQueryService queryService,
        IMedicalCaseStateService stateService,
        IMedicalCasePermissionService permissionService,
        IMedicalCaseAuditService auditService)
    {
        _commandService = commandService;
        _queryService = queryService;
        _stateService = stateService;
        _permissionService = permissionService;
        _auditService = auditService;
    }

    // ===== 写操作 - 委托 CommandService =====

    public Task<MedicalCase?> SaveAsync(MedicalCaseInputDto input, Guid operatorId, bool isAdmin)
        => _commandService.SaveAsync(input, operatorId, isAdmin);

    public Task<MedicalCase?> SetPrescriptionFlagAsync(Guid id, bool flag, Guid operatorId, bool isAdmin)
        => _commandService.SetPrescriptionFlagAsync(id, flag, operatorId, isAdmin);

    public Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin)
        => _commandService.DeleteAsync(id, operatorId, isAdmin);

    public Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId, bool isAdmin)
        => _commandService.BatchDeleteAsync(ids, operatorId, isAdmin);

    public Task<MedicalCase?> RecordPrintCompletedAsync(
        Guid medicalCaseId,
        LYBT.Shared.Models.Enums.PrintType printType,
        Guid printedBy,
        string printedByName,
        string? printerName = null)
        => _commandService.RecordPrintCompletedAsync(medicalCaseId, printType, printedBy, printedByName, printerName);

    /// <inheritdoc />
    public Task<bool> AddPrintLogAsync(
        Guid medicalCaseId,
        LYBT.Shared.Models.Enums.PrintType printType,
        bool isSuccess,
        Guid printedBy,
        string printedByName,
        string? printerName = null,
        string? errorMessage = null)
        => _commandService.AddPrintLogAsync(medicalCaseId, printType, isSuccess, printedBy, printedByName, printerName, errorMessage);

    // ===== 状态操作 - 委托 StateService =====

    public Task<MedicalCase?> UpdateStatusAsync(Guid id, MedicalCaseStatus status)
        => _stateService.UpdateStatusAsync(id, status);

    public Task<MedicalCase?> CompleteAsync(Guid id, Guid operatorId, bool isAdmin, bool skipWorkflowValidation = false)
        => _stateService.CompleteAsync(id, operatorId, isAdmin, skipWorkflowValidation);

    public Task<MedicalCase?> SuspendAsync(Guid id, ConsultationInputDto? input, Guid operatorId, bool isAdmin)
        => _stateService.SuspendAsync(id, input, operatorId, isAdmin);

    public Task<MedicalCase?> CancelAsync(Guid id, Guid operatorId, bool isAdmin, string? reason = null)
        => _stateService.CancelAsync(id, operatorId, isAdmin, reason);

    // ===== 读操作 - 委托 QueryService =====

    public Task<MedicalCase?> GetByIdAsync(Guid id)
        => _queryService.GetByIdAsync(id);

    public Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync(
        MedicalCaseStatus? status, Guid? patientId, int page, int pageSize,
        Guid? currentDoctorId = null, bool isAdmin = false, string? keyword = null)
        => _queryService.GetListDtoAsync(status, patientId, page, pageSize, currentDoctorId, isAdmin, keyword);

    public Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId)
        => _queryService.GetConsultationListAsync(medicalCaseId);

    public Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId)
        => _queryService.GetPrescriptionListAsync(medicalCaseId);

    public Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null)
        => _queryService.GetPendingCasesAsync(doctorId, patientId);

    public Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync()
        => _queryService.GetAllPendingCasesAsync();

    public Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(
        string? patientName = null, string? diagnosisKeyword = null,
        DateTime? startDate = null, DateTime? endDate = null,
        int page = 1, int pageSize = 20)
        => _queryService.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);

    public Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
        => _queryService.QueryAsync(query);

    public Task<List<MedicalCase>> GetBatchAsync(List<Guid> ids)
        => _queryService.GetBatchAsync(ids);

    // ===== 权限 - 委托 PermissionService =====

    public MedicalCasePermissionDto GetPermissions(Guid userId, UserRole role, MedicalCase mc)
        => _permissionService.GetPermissions(userId, role, mc);

    // ===== 审计 - 委托 AuditService =====

    public Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetAuditLogsPagedAsync(
        Guid medicalCaseId, int page = 1, int pageSize = 20)
        => _auditService.GetLogsPagedAsync(medicalCaseId, page, pageSize);
}
