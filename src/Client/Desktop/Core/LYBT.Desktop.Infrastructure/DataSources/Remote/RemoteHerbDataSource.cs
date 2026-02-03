using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程药材数据源 - 通过 API 访问服务端
/// OpenSpec: implement-local-mode
/// </summary>
public class RemoteHerbDataSource : IHerbDataSource
{
    private readonly IHerbApi _api;
    private readonly ILogger<RemoteHerbDataSource> _logger;
    private readonly HerbDataSourceMapper _mapper = new();

    public RemoteHerbDataSource(IHerbApi api, ILogger<RemoteHerbDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Herb.GetById - Id={Id}", id);

        try
        {
            var response = await _api.GetHerbByIdAsync(id);
            if (response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Herb.GetById - NotFound: {Id}", id);
                return null;
            }
            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public Task<(List<Herb> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        return GetPagedAsync(page, pageSize, keyword, null, ct);
    }

    public async Task<(List<Herb> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? category,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Herb.GetPaged - Page={Page}, Category={Category}", page, category);

        try
        {
            var response = await _api.GetHerbsAsync(page, pageSize, keyword, category);
            if (response.Data == null)
            {
                return (new List<Herb>(), 0);
            }

            var items = response.Data.Items.Select(_mapper.ToEntity).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.GetPaged failed");
            throw;
        }
    }

    public async Task<Herb> CreateAsync(Herb entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.Create - Name={Name}", entity.Name);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            var response = await _api.CreateHerbAsync(inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建药材失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.Create failed");
            throw;
        }
    }

    public async Task<Herb> UpdateAsync(Herb entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.Update - Id={Id}", entity.Id);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            inputDto.Id = entity.Id;
            var response = await _api.UpdateHerbAsync(entity.Id, inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新药材失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.Update failed - Id={Id}", entity.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.Delete - Id={Id}", id);

        try
        {
            var response = await _api.DeleteHerbAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.ToggleStatus - Id={Id}", id);

        try
        {
            var response = await _api.ToggleStatusAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.ToggleStatus failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<Herb?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.Restore - Id={Id}", id);

        try
        {
            var response = await _api.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] Herb.Restore failed: {Message}", response.Message);
                return null;
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<List<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Herb.GetCategories");

        try
        {
            // 获取所有药材并提取分类
            var response = await _api.GetHerbsAsync(1, 1000);
            if (response.Data == null)
            {
                return new List<string>();
            }

            return response.Data.Items
                .Select(h => h.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList()!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.GetCategories failed");
            throw;
        }
    }

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.BatchDelete - Count={Count}", ids.Count);

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
            _logger.LogError(ex, "[RemoteDataSource] Herb.BatchDelete failed");
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
