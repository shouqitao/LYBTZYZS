using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程药材数据源 - 通过 API 访问服务端
/// </summary>
public class RemoteHerbDataSource : IHerbDataSource
{
    private readonly IHerbApi _api;
    private readonly ILogger<RemoteHerbDataSource> _logger;
    private readonly HerbListToDetailMapper _listMapper = new();

    public RemoteHerbDataSource(IHerbApi api, ILogger<RemoteHerbDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<HerbDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
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
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public Task<(List<HerbDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        return GetPagedAsync(page, pageSize, keyword, null, ct);
    }

    public async Task<(List<HerbDetailDto> Items, int Total)> GetPagedAsync(
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
                return (new List<HerbDetailDto>(), 0);
            }

            var items = response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.GetPaged failed");
            throw;
        }
    }

    public async Task<HerbDetailDto> CreateAsync(HerbInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.Create - Name={Name}", input.Name);

        try
        {
            var response = await _api.CreateHerbAsync(input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建药材失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.Create failed");
            throw;
        }
    }

    public async Task<HerbDetailDto> UpdateAsync(HerbInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.Update - Id={Id}", input.Id);

        try
        {
            var response = await _api.UpdateHerbAsync(input.Id!.Value, input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新药材失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.Update failed - Id={Id}", input.Id);
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

    public async Task<HerbDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
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

            return response.Data;
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
    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <inheritdoc />
    public async Task<BatchOperationResultDto> BatchToggleStatusAsync(List<Guid> ids, bool enable, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.BatchToggleStatus - Count={Count}, Enable={Enable}", ids.Count, enable);

        try
        {
            var input = new BatchDeleteInputDto { Ids = ids };
            var response = enable
                ? await _api.BatchEnableAsync(input)
                : await _api.BatchDisableAsync(input);

            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量切换状态失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.BatchToggleStatus failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<BatchOperationResultDto> BatchImportAsync(List<HerbInputDto> items, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] Herb.BatchImport - Count={Count}", items.Count);

        // 远程模式逐个创建（API 无批量导入 HerbInputDto 接口）
        var result = new BatchOperationResultDto
        {
            TotalCount = items.Count,
            IsSuccess = true
        };

        foreach (var item in items)
        {
            try
            {
                var created = await CreateAsync(item, ct);
                result.SuccessCount++;
                result.SuccessfulIds.Add(created.Id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(new BatchOperationFailureItem
                {
                    Reason = $"导入药材 '{item.Name}' 失败: {ex.Message}"
                });
                _logger.LogWarning(ex, "[RemoteDataSource] Herb.BatchImport - Failed to import: {Name}", item.Name);
            }
        }

        result.IsSuccess = result.FailureCount == 0;
        return result;
    }

    /// <inheritdoc />
    public async Task<List<HerbDetailDto>> GetAllForExportAsync(string? keyword = null, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Herb.GetAllForExport - Keyword={Keyword}", keyword);

        try
        {
            var response = await _api.GetHerbsAsync(1, 10000, keyword);
            if (response.Data == null)
            {
                return new List<HerbDetailDto>();
            }

            return response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] Herb.GetAllForExport failed");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> HasReferencesAsync(Guid herbId, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] Herb.HasReferences - HerbId={HerbId}", herbId);

        // 远程模式保守返回 true（由服务端判断）
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public string[] GetImportTemplateColumns()
    {
        return ["药材名称", "拼音码", "分类", "性味", "归经", "功效", "单位", "单价"];
    }
}

/// <summary>
/// HerbListDto -> HerbDetailDto 映射器 (仅限 DTO 间转换, 无 Entity 依赖)
/// </summary>
[Mapper]
internal partial class HerbListToDetailMapper
{
    [MapperIgnoreTarget(nameof(HerbDetailDto.Properties))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.CostPrice))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.Effect))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.Usage))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.Remark))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.CreatedBy))]
    public partial HerbDetailDto ToDetailDto(HerbListDto listDto);
}
