using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程患者数据源实现 - 调用 WebAPI
/// </summary>
public class RemotePatientDataSource : IPatientDataSource
{
    private readonly IPatientApi _api;
    private readonly ILogger<RemotePatientDataSource> _logger;
    private readonly PatientListToDetailMapper _listMapper = new();

    public RemotePatientDataSource(IPatientApi api, ILogger<RemotePatientDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.GetById - Id={Id}", id);

        try
        {
            var response = await _api.GetPatientByIdAsync(id);
            if (response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Patient.GetById - NotFound: {Id}", id);
                return null;
            }
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<(List<PatientDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.GetPaged - Page={Page}, Keyword={Keyword}", page, keyword);

        try
        {
            var response = await _api.GetPatientsAsync(page, pageSize, keyword);
            if (response.Data == null)
            {
                return (new List<PatientDetailDto>(), 0);
            }

            var items = response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.GetPaged failed");
            throw;
        }
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.Create - Name={Name}", input.Name);

        try
        {
            var response = await _api.CreatePatientAsync(input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建患者失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Create failed");
            throw;
        }
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.Update - Id={Id}", input.Id);

        try
        {
            var response = await _api.UpdatePatientAsync(input.Id!.Value, input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新患者失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Update failed - Id={Id}", input.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.Delete - Id={Id}", id);

        try
        {
            var response = await _api.DeletePatientAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<PatientDetailDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.Search - Keyword={Keyword}", keyword);

        try
        {
            var response = await _api.GetPatientsAsync(1, 100, keyword);
            if (response.Data == null)
            {
                return new List<PatientDetailDto>();
            }

            return response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Search failed");
            throw;
        }
    }

    public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.GetByIdNumber");

        try
        {
            var candidates = await SearchAsync(idNumber, ct);
            if (candidates.Count == 0)
                return null;

            // 精确匹配 - 列表DTO没有IdNumber, 需要获取详情
            foreach (var candidate in candidates)
            {
                var detail = await GetByIdAsync(candidate.Id, ct);
                if (detail?.IdNumber?.Equals(idNumber, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return detail;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.GetByIdNumber failed");
            return null;
        }
    }

    public async Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.Restore - Id={Id}", id);

        try
        {
            var response = await _api.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Patient.Restore failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.BatchDelete - Count={Count}", ids.Count);

        try
        {
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
            _logger.LogError(ex, "[RemoteDataSource] Patient.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }
    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <inheritdoc />
    public async Task<BatchOperationResultDto> BatchImportAsync(List<PatientInputDto> items, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.BatchImport - Count={Count}", items.Count);

        try
        {
            var response = await _api.BatchImportAsync(new PatientBatchImportInputDto
            {
                Patients = items
            });

            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = items.Count,
                    FailureCount = items.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量导入失败"
                };
            }

            // 将 PatientBatchImportResultDto 转换为 BatchOperationResultDto
            return new BatchOperationResultDto
            {
                TotalCount = response.Data.TotalCount,
                SuccessCount = response.Data.SuccessCount,
                FailureCount = response.Data.FailureCount,
                IsSuccess = response.Data.FailureCount == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.BatchImport failed");
            return new BatchOperationResultDto
            {
                TotalCount = items.Count,
                FailureCount = items.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<PatientDetailDto>> GetAllForExportAsync(string? keyword = null, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.GetAllForExport - Keyword={Keyword}", keyword);

        try
        {
            // 使用大 pageSize 获取所有数据
            var response = await _api.GetPatientsAsync(1, 10000, keyword);
            if (response.Data == null)
            {
                return new List<PatientDetailDto>();
            }

            return response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.GetAllForExport failed");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> HasMedicalCasesAsync(Guid patientId, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.HasMedicalCases - PatientId={PatientId}", patientId);

        // 远程模式保守返回 true（由服务端判断）
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<Dictionary<Guid, bool>> BatchCheckReferencesAsync(List<Guid> patientIds, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.BatchCheckReferences - Count={Count}", patientIds.Count);

        // 远程模式保守返回 true（由服务端判断）
        var result = patientIds.ToDictionary(id => id, _ => true);
        return Task.FromResult(result);
    }
}

/// <summary>
/// PatientListDto -> PatientDetailDto 映射器 (仅限 DTO 间转换, 无 Entity 依赖)
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
