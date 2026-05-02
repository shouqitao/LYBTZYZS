using System.IO;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories;

/// <summary>
/// 药材仓储 - 通过 Refit IHerbApi 访问 WebAPI。
/// </summary>
public sealed class HerbRepository : IHerbRepository
{
    private readonly IHerbApi _api;
    private readonly ILocalHerbApi _localApi;
    private readonly IApiRouter _apiRouter;
    private readonly ILogger<HerbRepository> _logger;

    private bool IsOffline => _apiRouter.IsOffline;

    public HerbRepository(
        IHerbApi api,
        ILocalHerbApi localApi,
        IApiRouter apiRouter,
        ILogger<HerbRepository> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _localApi = localApi ?? throw new ArgumentNullException(nameof(localApi));
        _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Herb.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}", page, pageSize, keyword);
                var herbs = await _localApi.GetHerbsAsync(keyword);
                return new PagedResult<HerbListDto>
                {
                    Items = herbs,
                    TotalCount = herbs.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            _logger.LogDebug("[REPO:Remote] Herb.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                page, pageSize, keyword, category);

            var response = await _api.GetHerbsAsync(page, pageSize, keyword, category);
            if (response.Data == null)
                return new PagedResult<HerbListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

            return new PagedResult<HerbListDto>
            {
                Items = response.Data.Items.ToList(),
                TotalCount = response.Data.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.GetPaged failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    public async Task<HerbDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Herb.GetById - Id={Id}", id);
                return await _localApi.GetHerbByIdAsync(id);
            }

            _logger.LogDebug("[REPO:Remote] Herb.GetById - Id={Id}", id);
            var response = await _api.GetHerbByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.GetById failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    public async Task<HerbDetailDto> CreateAsync(HerbInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.Create - Name={Name}", dto.Name);
                return await _localApi.CreateHerbAsync(dto);
            }

            _logger.LogInformation("[REPO:Remote] Herb.Create started - Name={Name}", dto.Name);

            var response = await _api.CreateHerbAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建药材失败");

            _logger.LogInformation("[REPO:Remote] Herb.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.Create failed - Name={Name}", IsOffline ? "Local" : "Remote", dto.Name);
            throw;
        }
    }

    public async Task<HerbDetailDto> UpdateAsync(HerbInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.Update - Id={Id}", dto.Id);
                return await _localApi.UpdateHerbAsync(dto.Id.Value, dto);
            }

            _logger.LogInformation("[REPO:Remote] Herb.Update - Id={Id}", dto.Id);

            var response = await _api.UpdateHerbAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新药材失败");

            _logger.LogInformation("[REPO:Remote] Herb.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.Update failed - Id={Id}", IsOffline ? "Local" : "Remote", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.Delete - Id={Id}", id);
                await _localApi.DeleteHerbAsync(id);
                return true;
            }

            _logger.LogInformation("[REPO:Remote] Herb.Delete - Id={Id}", id);

            var response = await _api.DeleteHerbAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO:Remote] Herb.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO:Remote] Herb.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.Delete failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return false;
        }
    }

    public async Task<List<HerbListDto>> SearchAsync(string keyword)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Herb.Search - Keyword={Keyword}", keyword);
                return await _localApi.GetHerbsAsync(keyword);
            }

            _logger.LogDebug("[REPO:Remote] Herb.Search - Keyword={Keyword}", keyword);

            var response = await _api.GetHerbsAsync(1, 100, keyword);
            if (response.Data == null)
                return [];

            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.Search failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    #endregion

    #region 批量导入/导出功能

    public async Task<HerbBatchImportResultDto?> BatchImportAsync(Stream fileStream, string fileName)
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Herb.BatchImport not supported in offline mode");
            return null;
        }

        try
        {
            _logger.LogInformation("[REPO:Remote] Herb.BatchImport - FileName={FileName}", fileName);

            var streamPart = new Refit.StreamPart(fileStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            var response = await _api.BatchImportAsync(streamPart);

            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO:Remote] Herb.BatchImport failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] Herb.BatchImport completed - Success={SuccessCount} Failure={FailureCount}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Herb.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync()
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Herb.ExportTemplate not supported in offline mode");
            return null;
        }

        try
        {
            _logger.LogInformation("[REPO:Remote] Herb.ExportTemplate");

            var response = await _api.ExportTemplateAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[REPO:Remote] Herb.ExportTemplate failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("[REPO:Remote] Herb.ExportTemplate completed - Size={Size} bytes", bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Herb.ExportTemplate failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportHerbsAsync(string? keyword = null)
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Herb.ExportHerbs not supported in offline mode");
            return null;
        }

        try
        {
            _logger.LogInformation("[REPO:Remote] Herb.ExportHerbs - Keyword={Keyword}", keyword ?? "全部");

            var response = await _api.ExportHerbsAsync(keyword);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[REPO:Remote] Herb.ExportHerbs failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("[REPO:Remote] Herb.ExportHerbs completed - Size={Size} bytes", bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Herb.ExportHerbs failed");
            return null;
        }
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<HerbDetailDto?> ToggleStatusAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.ToggleStatus - Id={Id}", id);
                return await _localApi.ToggleStatusAsync(id);
            }

            _logger.LogInformation("[REPO:Remote] Herb.ToggleStatus - Id={Id}", id);

            var response = await _api.ToggleStatusAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Herb.ToggleStatus failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] Herb.ToggleStatus completed - Status={Status}", response.Data.Status);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.ToggleStatus failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return null;
        }
    }

    public async Task<HerbDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.Restore - Id={Id}", id);
                return await _localApi.RestoreAsync(id);
            }

            _logger.LogInformation("[REPO:Remote] Herb.Restore - Id={Id}", id);

            var response = await _api.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Herb.Restore failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.Restore failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.BatchDelete - Count={Count}", ids.Count);
                return await _localApi.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            }

            _logger.LogInformation("[REPO:Remote] Herb.BatchDelete - Count={Count}", ids.Count);

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
            _logger.LogError(ex, "[REPO:{Mode}] Herb.BatchDelete failed", IsOffline ? "Local" : "Remote");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.BatchEnable - Count={Count}", ids.Count);
                return await _localApi.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
            }

            _logger.LogInformation("[REPO:Remote] Herb.BatchEnable - Count={Count}", ids.Count);

            var response = await _api.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO:Remote] Herb.BatchEnable failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] Herb.BatchEnable completed - Success={Success} Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.BatchEnable failed", IsOffline ? "Local" : "Remote");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Herb.BatchDisable - Count={Count}", ids.Count);
                return await _localApi.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
            }

            _logger.LogInformation("[REPO:Remote] Herb.BatchDisable - Count={Count}", ids.Count);

            var response = await _api.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO:Remote] Herb.BatchDisable failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] Herb.BatchDisable completed - Success={Success} Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.BatchDisable failed", IsOffline ? "Local" : "Remote");
            return null;
        }
    }

    #endregion

    #region 包装方法 (统一返回元组格式)

    public async Task<(bool success, HerbDetailDto? data, string? error)> CreateWithResultAsync(HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[REPO:{Mode}] Herb.CreateWithResult - Name={Name}", IsOffline ? "Local" : "Remote", input.Name);
            var result = await CreateAsync(input);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.CreateWithResult failed - Name={Name}", IsOffline ? "Local" : "Remote", input.Name);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建中药", ex));
        }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> UpdateWithResultAsync(Guid id, HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[REPO:{Mode}] Herb.UpdateWithResult - Id={Id}", IsOffline ? "Local" : "Remote", id);
            var result = await UpdateAsync(input);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.UpdateWithResult failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新中药", ex));
        }
    }

    public async Task<(bool success, string? error)> DeleteWithResultAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:{Mode}] Herb.DeleteWithResult - Id={Id}", IsOffline ? "Local" : "Remote", id);
            var result = await DeleteAsync(id);

            if (result)
                return (true, null);
            else
                return (false, "删除中药失败，记录不存在或已被删除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.DeleteWithResult failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除中药", ex));
        }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> GetByIdWithResultAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:{Mode}] Herb.GetByIdWithResult - Id={Id}", IsOffline ? "Local" : "Remote", id);
            var result = await GetByIdAsync(id);

            if (result != null)
                return (true, result, null);
            else
                return (false, null, "未找到指定的中药记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Herb.GetByIdWithResult failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取中药详情", ex));
        }
    }

    #endregion
}
