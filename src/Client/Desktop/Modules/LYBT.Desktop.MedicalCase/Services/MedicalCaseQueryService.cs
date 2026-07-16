using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案查询服务 - 读操作
/// 从 MedicalCaseService 拆分，实现 IMedicalCaseQueryService
/// </summary>
internal class MedicalCaseQueryService : IMedicalCaseQueryService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly ILogger<MedicalCaseQueryService> _logger;

    public MedicalCaseQueryService(
        IMedicalCaseRepository repository,
        ILogger<MedicalCaseQueryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[Query] MedicalCase.GetPaged started - Page={Page} PageSize={PageSize}", page, pageSize);
            var result = await _repository.GetPagedAsync(page, pageSize, searchText);
            _logger.LogDebug("[Query] MedicalCase.GetPaged completed - TotalCount={TotalCount}", result?.TotalCount ?? 0);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[Query] MedicalCase.GetPaged failed - Page={Page}", page); return null; }
    }

    public virtual async Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[Query] MedicalCase.Query started - QueryType={QueryType}", query.QueryType);
            var result = await _repository.QueryAsync(query);
            _logger.LogDebug("[Query] MedicalCase.Query completed - TotalCount={TotalCount}", result?.TotalCount ?? 0);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[Query] MedicalCase.Query failed - QueryType={QueryType}", query.QueryType); return null; }
    }

    public virtual async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[Query] MedicalCase.GetUnfinishedByPatient started - PatientId={PatientId} DoctorId={DoctorId}", patientId, doctorId);

            var query = new MedicalCaseQueryDto
            {
                QueryType = LYBT.Shared.Models.Enums.MedicalCaseQueryType.Unfinished,
                PatientId = patientId,
                DoctorId = doctorId,
                IncludeAllDoctors = checkAllDoctors,
                PageSize = 1
            };
            var result = await _repository.QueryAsync(query);

            if (result?.Items?.Count > 0)
            {
                var detail = await _repository.GetByIdAsync(result.Items[0].Id);
                _logger.LogDebug("[Query] MedicalCase.GetUnfinishedByPatient found - MedicalCaseId={MedicalCaseId}", detail?.Id);
                return detail;
            }

            _logger.LogDebug("[Query] MedicalCase.GetUnfinishedByPatient → NotFound - PatientId={PatientId}", patientId);
            return null;
        }
        catch (Exception ex) { _logger.LogError(ex, "[Query] MedicalCase.GetUnfinishedByPatient failed - PatientId={PatientId}", patientId); throw; }
    }
}
