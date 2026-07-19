using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories;

/// <summary>
/// 药材仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class HerbRepository : ApiClientRepositoryBase<HerbListDto, HerbDetailDto, HerbInputDto, HerbInputDto>, IHerbRepository
{
    private readonly IApiClient _apiClient;

    public HerbRepository(
        IApiClient apiClient,
        ILogger<HerbRepository> logger)
        : base(logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    protected override string LogPrefix => "Herb";

    #region 标准 CRUD 操作

    public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Herbs.GetHerbsAsync(page, pageSize, keyword, category);
                if (response.Data == null)
                    return new PagedResult<HerbListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

                return new PagedResult<HerbListDto>
                {
                    Items = response.Data.Items.ToList(),
                    TotalCount = response.Data.TotalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            },
            "GetPaged",
            "[REPO] Herb.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
            [page, pageSize, keyword, category]);
    }

    public async Task<HerbDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Herbs.GetHerbByIdAsync(id);
                return response.Data;
            },
            "GetById");
    }

    public async Task<HerbDetailDto> CreateAsync(HerbInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Herbs.CreateHerbAsync(dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "创建药材失败");

                Logger.LogInformation("[REPO] Herb.Create completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Create",
            LogLevel.Information);
    }

    public async Task<HerbDetailDto> UpdateAsync(HerbInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Herbs.UpdateHerbAsync(dto.Id.Value, dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "更新药材失败");

                Logger.LogInformation("[REPO] Herb.Update completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Update",
            LogLevel.Information);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // DeleteAsync returns false on exception instead of rethrowing,
        // which differs from ExecuteAsync's always-rethrow behavior.
        try
        {
            Logger.LogInformation("[REPO] Herb.Delete - Id={Id}", id);

            var response = await _apiClient.Herbs.DeleteHerbAsync(id);
            if (response.Success)
                Logger.LogInformation("[REPO] Herb.Delete completed - Id={Id}", id);
            else
                Logger.LogWarning("[REPO] Herb.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Herb.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<HerbListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Herbs.GetHerbsAsync(1, 100, keyword);
                if (response.Data == null)
                    return [];

                return response.Data.Items.ToList();
            },
            "Search");
    }

    #endregion

    #region 批量导入/导出功能

    public async Task<HerbBatchImportResultDto?> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Herb.BatchImport - Count={Count}", request.Herbs.Count);

            var response = await _apiClient.Herbs.BatchImportAsync(request);
            if (!response.Success || response.Data == null)
            {
                Logger.LogError("[REPO] Herb.BatchImport failed: {Message}", response.Message);
                return null;
            }

            Logger.LogInformation("[REPO] Herb.BatchImport completed - Success={SuccessCount} Failure={FailureCount}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Herb.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Herb.ExportTemplate");

            var response = await _apiClient.Herbs.ExportTemplateAsync();
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("[REPO] Herb.ExportTemplate failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Logger.LogInformation("[REPO] Herb.ExportTemplate completed - Size={Size} bytes", bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Herb.ExportTemplate failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportHerbsAsync(string? keyword = null, CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Herb.ExportHerbs - Keyword={Keyword}", keyword ?? "全部");

            var response = await _apiClient.Herbs.ExportHerbsAsync(keyword);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("[REPO] Herb.ExportHerbs failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Logger.LogInformation("[REPO] Herb.ExportHerbs completed - Size={Size} bytes", bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Herb.ExportHerbs failed");
            return null;
        }
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<HerbDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Herb.ToggleStatus - Id={Id}", id);

            var response = await _apiClient.Herbs.ToggleStatusAsync(id);
            if (!response.Success || response.Data == null)
            {
                Logger.LogWarning("[REPO] Herb.ToggleStatus failed: {Message}", response.Message);
                return null;
            }

            Logger.LogInformation("[REPO] Herb.ToggleStatus completed - Status={Status}", response.Data.Status);
            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Herb.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        // Returns failure DTO on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Herb.BatchDelete - Count={Count}", ids.Count);

            var response = await _apiClient.Herbs.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
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
            Logger.LogError(ex, "[REPO] Herb.BatchDelete failed");
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
