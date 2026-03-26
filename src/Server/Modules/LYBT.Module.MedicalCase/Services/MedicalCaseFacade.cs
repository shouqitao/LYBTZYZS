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
/// MedicalCase 门面服务实现 - 聚合 6 个 CQRS 服务的简单委托
/// 降低 Controller 构造函数依赖数量 (8 -> 3)
/// </summary>
public class MedicalCaseFacade : IMedicalCaseFacade
{
    private readonly IMedicalCaseCommandService _commandService;
    private readonly IMedicalCaseQueryService _queryService;
    private readonly IMedicalCaseStateService _stateService;
    private readonly IMedicalCasePermissionService _permissionService;
    private readonly IMedicalCaseAuditService _auditService;
    private readonly IMedicalCasePrintService _printService;

    public MedicalCaseFacade(
        IMedicalCaseCommandService commandService,
        IMedicalCaseQueryService queryService,
        IMedicalCaseStateService stateService,
        IMedicalCasePermissionService permissionService,
        IMedicalCaseAuditService auditService,
        IMedicalCasePrintService printService)
    {
        _commandService = commandService;
        _queryService = queryService;
        _stateService = stateService;
        _permissionService = permissionService;
        _auditService = auditService;
        _printService = printService;
    }

    // ===== 写操作 - 委托 CommandService =====

    public Task<MedicalCase?> SaveAsync(MedicalCaseInputDto input, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)
        => _commandService.SaveAsync(input, operatorId, isAdmin, cancellationToken);

    public Task<MedicalCase?> SetPrescriptionFlagAsync(Guid id, bool flag, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)
        => _commandService.SetPrescriptionFlagAsync(id, flag, operatorId, isAdmin, cancellationToken);

    public Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)
        => _commandService.DeleteAsync(id, operatorId, isAdmin, cancellationToken);

    public Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)
        => _commandService.BatchDeleteAsync(ids, operatorId, isAdmin, cancellationToken);

    public Task<MedicalCase?> RecordPrintCompletedAsync(
        Guid medicalCaseId,
        LYBT.Shared.Models.Enums.PrintType printType,
        Guid printedBy,
        string printedByName,
        string? printerName = null,
        CancellationToken cancellationToken = default)
        => _printService.RecordPrintCompletedAsync(medicalCaseId, printType, printedBy, printedByName, printerName, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AddPrintLogAsync(
        Guid medicalCaseId,
        LYBT.Shared.Models.Enums.PrintType printType,
        bool isSuccess,
        Guid printedBy,
        string printedByName,
        string? printerName = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
        => _printService.AddPrintLogAsync(medicalCaseId, printType, isSuccess, printedBy, printedByName, printerName, errorMessage, cancellationToken);

    // ===== 状态操作 - 委托 StateService =====

    public Task<MedicalCase?> UpdateStatusAsync(Guid id, MedicalCaseStatus status, CancellationToken cancellationToken = default)
        => _stateService.UpdateStatusAsync(id, status, cancellationToken);

    public Task<MedicalCase?> CompleteAsync(Guid id, Guid operatorId, bool isAdmin, bool skipWorkflowValidation = false, CancellationToken cancellationToken = default)
        => _stateService.CompleteAsync(id, operatorId, isAdmin, skipWorkflowValidation, cancellationToken);

    public Task<MedicalCase?> SuspendAsync(Guid id, ConsultationInputDto? input, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)
        => _stateService.SuspendAsync(id, input, operatorId, isAdmin, cancellationToken);

    public Task<MedicalCase?> CancelAsync(Guid id, Guid operatorId, bool isAdmin, string? reason = null, CancellationToken cancellationToken = default)
        => _stateService.CancelAsync(id, operatorId, isAdmin, reason, cancellationToken);

    // ===== 读操作 - 委托 QueryService =====

    public Task<MedicalCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _queryService.GetByIdAsync(id, cancellationToken);

    public Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync(
        MedicalCaseStatus? status, Guid? patientId, int page, int pageSize,
        Guid? currentDoctorId = null, bool isAdmin = false, string? keyword = null,
        CancellationToken cancellationToken = default)
        => _queryService.GetListDtoAsync(status, patientId, page, pageSize, currentDoctorId, isAdmin, keyword, cancellationToken);

    public Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        => _queryService.GetConsultationListAsync(medicalCaseId, cancellationToken);

    public Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        => _queryService.GetPrescriptionListAsync(medicalCaseId, cancellationToken);

    public Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = default)
        => _queryService.GetPendingCasesAsync(doctorId, patientId, cancellationToken);

    public Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default)
        => _queryService.GetAllPendingCasesAsync(cancellationToken);

    public Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(
        string? patientName = null, string? diagnosisKeyword = null,
        DateTime? startDate = null, DateTime? endDate = null,
        int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
        => _queryService.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize, cancellationToken);

    public Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)
        => _queryService.QueryAsync(query, cancellationToken);

    public Task<List<MedicalCase>> GetBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        => _queryService.GetBatchAsync(ids, cancellationToken);

    // ===== 权限 - 委托 PermissionService =====

    public MedicalCasePermissionDto GetPermissions(Guid userId, UserRole role, MedicalCase mc)
        => _permissionService.GetPermissions(userId, role, mc);

    // ===== 审计 - 委托 AuditService =====

    public Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetAuditLogsPagedAsync(
        Guid medicalCaseId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        => _auditService.GetLogsPagedAsync(medicalCaseId, page, pageSize, cancellationToken);
}
