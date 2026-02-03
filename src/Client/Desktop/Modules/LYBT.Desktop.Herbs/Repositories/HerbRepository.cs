using System.IO;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories
{
    /// <summary>
    /// 药材数据仓储实现 - DataSource 抽象层重构
    /// OpenSpec: implement-local-mode - 支持 Local/Remote 模式切换
    /// </summary>
    public class HerbRepository : IHerbRepository
    {
        private readonly IHerbDataSource _dataSource;
        private readonly IHerbApi? _api; // 可选，仅用于批量导入/导出等 Remote 模式特有功能
        private readonly ILogger<HerbRepository> _logger;
        private readonly HerbDataSourceMapper _mapper = new();

        /// <summary>
        /// 初始化 HerbRepository
        /// </summary>
        /// <param name="dataSource">药材数据源（Local 或 Remote）</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="api">可选的 API 接口（仅 Remote 模式下注入，用于批量操作）</param>
        public HerbRepository(
            IHerbDataSource dataSource,
            ILogger<HerbRepository> logger,
            IHerbApi? api = null)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _api = api;
        }

        #region 标准 CRUD 操作

        /// <summary>
        /// 分页查询药材列表（支持分类过滤）
        /// </summary>
        public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                _logger.LogDebug("[REPO] Herb.GetPaged started - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                    page, pageSize, keyword, category);

                var (items, total) = await _dataSource.GetPagedAsync(page, pageSize, keyword, category);

                var listDtos = items.Select(e => new HerbListDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    PinYinCode = e.PinYinCode,
                    Category = e.Category,
                    Origin = e.Origin,
                    Spec = e.Spec,
                    Unit = e.Unit,
                    Price = e.Price,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt
                }).ToList();

                var result = new PagedResult<HerbListDto>
                {
                    Items = listDtos,
                    TotalCount = total,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogDebug("[REPO] Herb.GetPaged completed - TotalCount={TotalCount}", result.TotalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.GetPaged failed - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                    page, pageSize, keyword, category);
                throw;
            }
        }

        /// <summary>
        /// 根据 ID 获取药材详情
        /// </summary>
        public async Task<HerbDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("[REPO] Herb.GetById started - Id={Id}", id);

                var entity = await _dataSource.GetByIdAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("[REPO] Herb.GetById -> NotFound - Id={Id}", id);
                    return null;
                }

                var dto = _mapper.ToDetailDto(entity);
                _logger.LogDebug("[REPO] Herb.GetById completed - Id={Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.GetById failed - Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新药材
        /// </summary>
        public async Task<HerbDetailDto> CreateAsync(HerbInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] Herb.Create started - Name={Name}", dto.Name);

                var entity = _mapper.ToEntity(dto);
                var created = await _dataSource.CreateAsync(entity);
                var result = _mapper.ToDetailDto(created);

                _logger.LogInformation("[REPO] Herb.Create completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.Create failed - Name={Name}", dto.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新药材信息
        /// </summary>
        public async Task<HerbDetailDto> UpdateAsync(HerbInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.Id == null || dto.Id == Guid.Empty)
                throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] Herb.Update started - Id={Id}", dto.Id);

                var entity = _mapper.ToEntity(dto);
                var updated = await _dataSource.UpdateAsync(entity);
                var result = _mapper.ToDetailDto(updated);

                _logger.LogInformation("[REPO] Herb.Update completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.Update failed - Id={Id}", dto.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[REPO] Herb.Delete started - Id={Id}", id);

                var result = await _dataSource.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("[REPO] Herb.Delete completed - Id={Id}", id);
                }
                else
                {
                    _logger.LogWarning("[REPO] Herb.Delete -> Failed - Id={Id}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.Delete failed - Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 搜索药材（基于关键词，返回 ListDto）
        /// </summary>
        public async Task<List<HerbListDto>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogDebug("[REPO] Herb.Search started - Keyword={Keyword}", keyword);

                // 使用带分类过滤的分页方法，获取前100条
                var (items, _) = await _dataSource.GetPagedAsync(1, 100, keyword, null);

                var listDtos = items.Select(e => new HerbListDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    PinYinCode = e.PinYinCode,
                    Category = e.Category,
                    Origin = e.Origin,
                    Spec = e.Spec,
                    Unit = e.Unit,
                    Price = e.Price,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt
                }).ToList();

                _logger.LogDebug("[REPO] Herb.Search completed - Count={Count}", listDtos.Count);
                return listDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Herb.Search failed - Keyword={Keyword}", keyword);
                throw;
            }
        }

        #endregion

        #region 批量导入/导出功能 - 仅 Remote 模式支持

        /// <summary>
        /// 批量导入药材数据
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<HerbBatchImportResultDto?> BatchImportAsync(Stream fileStream, string fileName)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Herb.BatchImport -> NotSupported - 本地模式不支持批量导入");
                return null;
            }

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
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<byte[]?> ExportTemplateAsync()
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Herb.ExportTemplate -> NotSupported - 本地模式不支持导出模板");
                return null;
            }

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
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<byte[]?> ExportHerbsAsync(string? keyword = null)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Herb.ExportHerbs -> NotSupported - 本地模式不支持导出药材数据");
                return null;
            }

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

                var result = await _dataSource.ToggleStatusAsync(id);
                if (!result)
                {
                    _logger.LogError("切换药材状态失败：{Id}", id);
                    return null;
                }

                // 重新获取更新后的数据
                var entity = await _dataSource.GetByIdAsync(id);
                if (entity == null)
                {
                    return null;
                }

                var dto = _mapper.ToDetailDto(entity);
                _logger.LogInformation("药材状态已切换为：{Status}", dto.Status);
                return dto;
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

                var entity = await _dataSource.RestoreAsync(id);
                if (entity == null)
                {
                    _logger.LogError("恢复药材失败：{Id}", id);
                    return null;
                }

                var dto = _mapper.ToDetailDto(entity);
                _logger.LogInformation("药材已恢复：{Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复药材时发生异常：{Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 批量删除药材
        /// 注意：仅 Remote 模式支持批量 API 操作
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                // 本地模式：逐个删除
                _logger.LogInformation("批量删除药材（本地模式）：{Count}个", ids.Count);
                var successCount = 0;
                var failureCount = 0;

                foreach (var id in ids)
                {
                    var result = await _dataSource.DeleteAsync(id);
                    if (result)
                        successCount++;
                    else
                        failureCount++;
                }

                _logger.LogInformation("批量删除药材完成：成功{Success}个，失败{Failure}个", successCount, failureCount);
                return new BatchOperationResultDto { SuccessCount = successCount, FailureCount = failureCount };
            }

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

        /// <summary>
        /// 批量启用药材
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Herb.BatchEnable -> NotSupported - 本地模式不支持批量启用");
                return null;
            }

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

        /// <summary>
        /// 批量禁用药材
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Herb.BatchDisable -> NotSupported - 本地模式不支持批量禁用");
                return null;
            }

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
                    _logger.LogWarning("[REPO] Herb.Delete -> NotFound - HerbId={HerbId}", id);
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
                    _logger.LogWarning("[REPO] Herb.GetById -> NotFound - HerbId={HerbId}", id);
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
