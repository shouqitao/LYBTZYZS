using System.Threading;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Patients.Repositories;

/// <summary>
/// 患者仓储 - 远程模式实现 (SYNC-D02)
/// 通过 Refit IPatientApi 访问 WebAPI，不再依赖 IPatientDataSource 中间层。
/// DI 工厂根据 IConnectionModeProvider 在远程模式下选择此实现。
/// </summary>
public sealed class PatientRepository : IPatientRepository
{
    private readonly IPatientApi _api;
    private readonly ILogger<PatientRepository> _logger;
    private readonly PatientListToDetailMapper _listMapper = new();

    public PatientRepository(
        IPatientApi api,
        ILogger<PatientRepository> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] Patient.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _api.GetPatientsAsync(page, pageSize, keyword);
            if (response.Data == null)
                return new PagedResult<PatientListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

            return new PagedResult<PatientListDto>
            {
                Items = response.Data.Items.ToList(),
                TotalCount = response.Data.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.GetPaged failed");
            throw;
        }
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] Patient.GetById - Id={Id}", id);

            var response = await _api.GetPatientByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);

        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.Create started");

            var response = await _api.CreatePatientAsync(patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建患者失败");

            _logger.LogInformation("[REPO:Remote] Patient.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.Create failed");
            throw;
        }
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);
        if (patient.Id is null || patient.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(patient));

        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.Update - Id={Id}", patient.Id);

            var response = await _api.UpdatePatientAsync(patient.Id.Value, patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新患者失败");

            _logger.LogInformation("[REPO:Remote] Patient.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.Update failed - Id={Id}", patient.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.Delete - Id={Id}", id);

            var response = await _api.DeletePatientAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO:Remote] Patient.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO:Remote] Patient.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] Patient.Search - Keyword={Keyword}", keyword);

            var response = await _api.GetPatientsAsync(1, 100, keyword);
            if (response.Data == null)
                return [];

            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.Search failed");
            throw;
        }
    }

    #endregion

    #region 身份证号查询

    public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return null;

        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.GetByIdNumber");

            // 远程模式: 先搜索候选，再精确匹配身份证号
            var response = await _api.GetPatientsAsync(1, 100, idNumber);
            if (response.Data == null)
                return null;

            foreach (var candidate in response.Data.Items)
            {
                var detail = await GetByIdAsync(candidate.Id, ct);
                if (detail?.IdNumber?.Equals(idNumber, StringComparison.OrdinalIgnoreCase) == true)
                    return detail;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.GetByIdNumber failed");
            return null;
        }
    }

    #endregion

    #region 批量导入/导出功能

    public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.BatchImport");
            var response = await _api.BatchImportAsync(request);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.ExportTemplate");

            var response = await _api.ExportTemplateAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[REPO:Remote] Patient.ExportTemplate failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("[REPO:Remote] Patient.ExportTemplate completed - Size={Size} bytes", bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.ExportTemplate failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.ExportPatients - Keyword={Keyword}", keyword ?? "全部");

            var response = await _api.ExportPatientsAsync(keyword);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[REPO:Remote] Patient.ExportPatients failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("[REPO:Remote] Patient.ExportPatients completed - Size={Size} bytes", bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.ExportPatients failed");
            return null;
        }
    }

    #endregion

    #region 恢复和批量操作

    public async Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.Restore - Id={Id}", id);

            var response = await _api.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Patient.Restore failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Patient.BatchDelete - Count={Count}", ids.Count);

            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量删除失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    #endregion
}

/// <summary>
/// PatientListDto -> PatientDetailDto 映射器 (Refit API 返回 ListDto 时需要转换)
/// SYNC-D02: 保留用于远程模式下 ListDto 获取后需要 DetailDto 的场景
/// </summary>
[Mapper]
internal partial class PatientListToDetailMapper
{
    [MapperIgnoreTarget(nameof(PatientDetailDto.BirthDate))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.IdNumber))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.MaritalStatus))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.IdType))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.BloodType))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.AllergyHistory))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.MedicalHistory))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactName))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactPhone))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactRelation))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.DisableReason))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.CreatedBy))]
    public partial PatientDetailDto ToDetailDto(PatientListDto listDto);
}
