using System.IO;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories
{
    /// <summary>
    /// 药材数据仓储实现 - RESTful设计
    /// List返回轻量ListDto，Detail返回完整DetailDto
    /// </summary>
    public class HerbRepository : RepositoryBase<HerbDetailDto, HerbListDto, HerbInputDto, HerbInputDto, IHerbApi>, IHerbRepository
    {
        public HerbRepository(
            IHerbApi herbApi,
            ILogger<HerbRepository> logger)
            : base(herbApi, logger)
        {
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<HerbDetailDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetHerbByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<HerbListDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetHerbsAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<HerbDetailDto>> CallApiCreateAsync(HerbInputDto dto)
        {
            return _api.CreateHerbAsync(dto);
        }

        protected override Task<ApiResponse<HerbDetailDto>> CallApiUpdateAsync(Guid id, HerbInputDto dto)
        {
            return _api.UpdateHerbAsync(id, dto);
        }

        protected override Task<ApiResponse> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteHerbAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(HerbInputDto dto)
        {
            return dto?.Id;
        }

        #endregion

        #region 扩展方法 - 支持分类过滤

        /// <summary>
        /// 分页查询药材列表（支持分类过滤）
        /// </summary>
        public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                var response = await _api.GetHerbsAsync(page, pageSize, keyword, category);
                return response.Data ?? new PagedResult<HerbListDto>
                {
                    Items = new List<HerbListDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败");
                throw;
            }
        }

        #endregion

        #region 批量导入/导出功能

        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        public async Task<HerbBatchImportResultDto?> BatchImportAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("开始批量导入药材：{FileName}", fileName);

                var streamPart = new Refit.StreamPart(fileStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                var response = await _api.BatchImportAsync(streamPart);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量导入药材失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量导入完成：成功{SuccessCount}条，失败{FailureCount}条，跳过{SkippedCount}条",
                    response.Data.SuccessCount, response.Data.FailureCount, response.Data.SkippedCount);

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入药材时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 下载药材导入模板
        /// </summary>
        public async Task<byte[]?> ExportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("下载药材导入模板");

                var response = await _api.ExportTemplateAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("下载模板失败：{StatusCode}", response.StatusCode);
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("模板下载成功，大小：{Size} bytes", bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载模板时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 导出药材数据到Excel
        /// </summary>
        public async Task<byte[]?> ExportHerbsAsync(string? keyword = null)
        {
            try
            {
                _logger.LogInformation("导出药材数据，关键词：{Keyword}", keyword ?? "全部");

                var response = await _api.ExportHerbsAsync(keyword);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("导出药材失败：{StatusCode}", response.StatusCode);
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("药材数据导出成功，大小：{Size} bytes", bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据时发生异常");
                return null;
            }
        }

        #endregion

        #region 状态切换、恢复和批量操作

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        public async Task<HerbDetailDto?> ToggleStatusAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("切换药材状态：{Id}", id);
                var response = await _api.ToggleStatusAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("切换药材状态失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("药材状态已切换为：{Status}", response.Data.Status);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换药材状态时发生异常：{Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 恢复已删除的药材
        /// </summary>
        public async Task<HerbDetailDto?> RestoreAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("恢复药材：{Id}", id);
                var response = await _api.RestoreAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("恢复药材失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("药材已恢复：{Id}", id);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复药材时发生异常：{Id}", id);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量删除药材：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDeleteAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量删除药材失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量删除药材完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除药材时发生异常");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量启用药材：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchEnableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量启用药材失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量启用药材完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用药材时发生异常");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量禁用药材：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDisableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量禁用药材失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量禁用药材完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用药材时发生异常");
                return null;
            }
        }

        #endregion

        #region 包装方法（统一返回元组格式）
        // OpenSpec: simplify-desktop-data-layer - 合并HerbService功能

        /// <inheritdoc/>
        public async Task<(bool success, HerbDetailDto? data, string? error)> CreateWithResultAsync(HerbInputDto input)
        {
            try
            {
                _logger.LogInformation("[REPO] Herb.Create started - Name={Name}", input.Name);
                var result = await CreateAsync(input);
                _logger.LogInformation("[REPO] Herb.Create completed - HerbId={HerbId}", result.Id);
                return (true, result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.Create failed - Name={Name}", input.Name);
                return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建中药", ex));
            }
        }

        /// <inheritdoc/>
        public async Task<(bool success, HerbDetailDto? data, string? error)> UpdateWithResultAsync(Guid id, HerbInputDto input)
        {
            try
            {
                _logger.LogInformation("[REPO] Herb.Update started - HerbId={HerbId}", id);
                var result = await UpdateAsync(input);
                _logger.LogInformation("[REPO] Herb.Update completed - HerbId={HerbId}", id);
                return (true, result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.Update failed - HerbId={HerbId}", id);
                return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新中药", ex));
            }
        }

        /// <inheritdoc/>
        public async Task<(bool success, string? error)> DeleteWithResultAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[REPO] Herb.Delete started - HerbId={HerbId}", id);
                var result = await DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("[REPO] Herb.Delete completed - HerbId={HerbId}", id);
                    return (true, null);
                }
                else
                {
                    _logger.LogWarning("[REPO] Herb.Delete → NotFound - HerbId={HerbId}", id);
                    return (false, "删除中药失败，记录不存在或已被删除");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.Delete failed - HerbId={HerbId}", id);
                return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除中药", ex));
            }
        }

        /// <inheritdoc/>
        public async Task<(bool success, HerbDetailDto? data, string? error)> GetByIdWithResultAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("[REPO] Herb.GetById started - HerbId={HerbId}", id);
                var result = await GetByIdAsync(id);

                if (result != null)
                {
                    _logger.LogDebug("[REPO] Herb.GetById completed - HerbId={HerbId}", id);
                    return (true, result, null);
                }
                else
                {
                    _logger.LogWarning("[REPO] Herb.GetById → NotFound - HerbId={HerbId}", id);
                    return (false, null, "未找到指定的中药记录");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.GetById failed - HerbId={HerbId}", id);
                return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取中药详情", ex));
            }
        }

        #endregion
    }
}
