using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Catalog.Repositories;

/// <summary>
/// 药材仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class HerbRepository : EntityApiClientRepositoryBase<HerbListDto, HerbDetailDto, HerbInputDto>, IHerbRepository
{
    private readonly IApiClientHerbs _herbs;

    public HerbRepository(
        IApiClientHerbs herbs,
        ILogger<HerbRepository> logger)
        : base(logger, herbs)
    {
        _herbs = herbs ?? throw new ArgumentNullException(nameof(herbs));
    }

    protected override string LogPrefix => "Herb";

    #region 搜索

    public async Task<List<HerbListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _herbs.GetHerbsAsync(1, 100, keyword);
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
        return await ExecuteImportAsync(
            () => _herbs.BatchImportAsync(request),
            "BatchImport",
            request.Herbs.Count,
            ct);
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Herb.ExportTemplate");

            var response = await _herbs.ExportTemplateAsync();
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

            var response = await _herbs.ExportHerbsAsync(keyword);
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
        return await ExecuteAsync(
            async () =>
            {
                var response = await _herbs.ToggleStatusAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "切换药材状态失败");

                Logger.LogInformation("[REPO] Herb.ToggleStatus completed - Status={Status}", response.Data.Status);
                return response.Data;
            },
            "ToggleStatus",
            LogLevel.Information);
    }

    public async Task<HerbDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _herbs.RestoreAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "恢复药材失败");

                Logger.LogInformation("[REPO] Herb.Restore completed - Id={Id}", id);
                return response.Data;
            },
            "Restore",
            LogLevel.Information);
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        return await ExecuteBatchDeleteAsync(
            () => _herbs.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids }),
            "BatchDelete",
            "批量删除失败",
            ids.Count);
    }

    /// <summary>批量启用/禁用药材。</summary>
    public async Task<BatchOperationResultDto?> BatchSetStatusAsync(List<Guid> ids, CommonStatus status, CancellationToken ct = default)
    {
        return await ExecuteBatchDeleteAsync(
            () => status == CommonStatus.Enabled
                ? _herbs.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids })
                : _herbs.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids }),
            "BatchSetStatus",
            status == CommonStatus.Enabled ? "批量启用失败" : "批量禁用失败",
            ids.Count);
    }

    #endregion
}
