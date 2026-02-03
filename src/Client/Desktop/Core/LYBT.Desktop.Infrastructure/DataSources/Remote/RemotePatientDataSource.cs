using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程患者数据源实现 - 调用 WebAPI
/// OpenSpec: implement-local-mode
/// </summary>
public class RemotePatientDataSource : IPatientDataSource
{
    private readonly IPatientApi _api;
    private readonly ILogger<RemotePatientDataSource> _logger;
    private readonly PatientDataSourceMapper _mapper = new();

    public RemotePatientDataSource(IPatientApi api, ILogger<RemotePatientDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
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
            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<(List<Patient> Items, int Total)> GetPagedAsync(
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
                return (new List<Patient>(), 0);
            }

            var items = response.Data.Items.Select(_mapper.ToEntity).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.GetPaged failed");
            throw;
        }
    }

    public async Task<Patient> CreateAsync(Patient entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.Create - Name={Name}", entity.Name);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            var response = await _api.CreatePatientAsync(inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建患者失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Create failed");
            throw;
        }
    }

    public async Task<Patient> UpdateAsync(Patient entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Patient.Update - Id={Id}", entity.Id);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            inputDto.Id = entity.Id;
            var response = await _api.UpdatePatientAsync(entity.Id, inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新患者失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Update failed - Id={Id}", entity.Id);
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

    public async Task<List<Patient>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.Search - Keyword={Keyword}", keyword);

        try
        {
            var response = await _api.GetPatientsAsync(1, 100, keyword);
            if (response.Data == null)
            {
                return new List<Patient>();
            }

            return response.Data.Items.Select(_mapper.ToEntity).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Patient.Search failed");
            throw;
        }
    }

    public async Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Patient.GetByIdNumber");

        try
        {
            // 使用身份证号搜索
            var candidates = await SearchAsync(idNumber, ct);
            if (candidates.Count == 0)
                return null;

            // 精确匹配
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

    public async Task<Patient?> RestoreAsync(Guid id, CancellationToken ct = default)
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

            return _mapper.ToEntity(response.Data);
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
}
