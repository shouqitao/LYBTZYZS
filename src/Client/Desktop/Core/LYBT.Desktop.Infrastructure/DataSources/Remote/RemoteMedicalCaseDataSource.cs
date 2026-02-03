using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程医案数据源 - 通过 API 访问服务端
/// OpenSpec: implement-local-mode
/// </summary>
public class RemoteMedicalCaseDataSource : IMedicalCaseDataSource
{
    private readonly IMedicalCaseApi _api;
    private readonly ILogger<RemoteMedicalCaseDataSource> _logger;
    private readonly MedicalCaseDataSourceMapper _mapper = new();

    public RemoteMedicalCaseDataSource(IMedicalCaseApi api, ILogger<RemoteMedicalCaseDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<MedicalCase?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetById - Id={Id}", id);

        try
        {
            var response = await _api.GetMedicalCaseByIdAsync(id);
            if (response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] MedicalCase.GetById - NotFound: {Id}", id);
                return null;
            }
            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCase> CreateAsync(MedicalCase entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Create - PatientId={PatientId}", entity.PatientId);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            var response = await _api.CreateMedicalCaseAsync(inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建医案失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Create failed");
            throw;
        }
    }

    public async Task<MedicalCase> UpdateAsync(MedicalCase entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Update - Id={Id}", entity.Id);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            var response = await _api.SaveAsync(entity.Id, inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新医案失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Update failed - Id={Id}", entity.Id);
            throw;
        }
    }

    public async Task<(List<MedicalCase> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetPaged - Page={Page}", page);

        try
        {
            var response = await _api.GetMedicalCasesAsync(page, pageSize, keyword);
            if (response.Data == null)
            {
                return (new List<MedicalCase>(), 0);
            }

            var items = response.Data.Items.Select(_mapper.ToEntity).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.GetPaged failed");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Delete - Id={Id}", id);

        try
        {
            var response = await _api.DeleteMedicalCaseAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<MedicalCase> SaveAsync(MedicalCase entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Save - Id={Id}", entity.Id);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            var response = await _api.SaveAsync(entity.Id, inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "保存医案失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Save failed - Id={Id}", entity.Id);
            throw;
        }
    }

    public async Task<bool> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Complete - Id={Id}", id);

        try
        {
            var response = await _api.CloseCaseAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Complete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<bool> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Cancel - Id={Id}", id);

        try
        {
            var request = reason != null ? new CancelMedicalCaseRequestDto { Reason = reason } : null;
            var response = await _api.CancelMedicalCaseAsync(id, request);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Cancel failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<MedicalCase?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetWithDetails - Id={Id}", id);

        // GetMedicalCaseByIdAsync 已包含完整详情
        return await GetByIdAsync(id, ct);
    }

    public async Task<(List<MedicalCase> Items, int Total)> QueryAsync(
        Guid? patientId = null,
        Guid? userId = null,
        MedicalCaseStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.Query - PatientId={PatientId}, Page={Page}", patientId, page);

        try
        {
            // 使用 QueryMedicalCasesAsync 统一查询端点
            var queryType = patientId.HasValue ? MedicalCaseQueryType.ByPatient : MedicalCaseQueryType.All;
            var response = await _api.QueryMedicalCasesAsync(
                queryType: queryType,
                patientId: patientId,
                doctorId: userId,
                pageIndex: page,
                pageSize: pageSize);

            if (response.Data == null)
            {
                return (new List<MedicalCase>(), 0);
            }

            var items = response.Data.Items.Select(_mapper.ToEntity).ToList();

            // 客户端过滤状态和日期（如果服务端不支持）
            if (status.HasValue)
            {
                items = items.Where(m => m.CaseStatus == status.Value).ToList();
            }
            if (startDate.HasValue)
            {
                items = items.Where(m => m.CreatedAt >= startDate.Value).ToList();
            }
            if (endDate.HasValue)
            {
                items = items.Where(m => m.CreatedAt <= endDate.Value).ToList();
            }

            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Query failed");
            throw;
        }
    }

    public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetByPatientId - PatientId={PatientId}", patientId);

        try
        {
            var response = await _api.QueryMedicalCasesAsync(
                queryType: MedicalCaseQueryType.ByPatient,
                patientId: patientId,
                pageSize: 1000);

            if (response.Data == null)
            {
                return new List<MedicalCase>();
            }

            return response.Data.Items.Select(_mapper.ToEntity).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.GetByPatientId failed");
            throw;
        }
    }

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.BatchDelete - Count={Count}", ids.Count);

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
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.BatchDelete failed");
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
