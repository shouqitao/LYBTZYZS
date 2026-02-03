using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories
{
    /// <summary>
    /// 验方数据仓储实现 - DataSource 抽象层重构
    /// OpenSpec: implement-local-mode - 支持 Local/Remote 模式切换
    /// </summary>
    public class FormulaRepository : IFormulaRepository
    {
        private readonly IFormulaDataSource _dataSource;
        private readonly IFormulaApi? _api; // 可选，仅用于批量操作等 Remote 模式特有功能
        private readonly ILogger<FormulaRepository> _logger;
        private readonly FormulaDataSourceMapper _mapper = new();

        /// <summary>
        /// 初始化 FormulaRepository
        /// </summary>
        /// <param name="dataSource">验方数据源（Local 或 Remote）</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="api">可选的 API 接口（仅 Remote 模式下注入，用于批量操作）</param>
        public FormulaRepository(
            IFormulaDataSource dataSource,
            ILogger<FormulaRepository> logger,
            IFormulaApi? api = null)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _api = api;
        }

        #region 标准 CRUD 操作

        /// <summary>
        /// 分页查询验方列表（支持分类过滤）
        /// </summary>
        public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                _logger.LogDebug("[REPO] Formula.GetPaged started - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                    page, pageSize, keyword, category);

                var (items, total) = await _dataSource.GetPagedAsync(page, pageSize, keyword, category);

                var listDtos = items.Select(e => new FormulaListDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Effect = e.Effect,
                    Indications = e.Indication,
                    Category = e.Category,
                    IsShared = e.IsShared,
                    ValidationStatus = e.ValidationStatus,
                    Status = e.Status,
                    HerbCount = e.Herbs?.Count ?? 0,
                    TotalPrice = 0, // 需要从药材库获取价格，暂时设为0
                    CreatedAt = e.CreatedAt
                }).ToList();

                var result = new PagedResult<FormulaListDto>
                {
                    Items = listDtos,
                    TotalCount = total,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogDebug("[REPO] Formula.GetPaged completed - TotalCount={TotalCount}", result.TotalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Formula.GetPaged failed - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                    page, pageSize, keyword, category);
                throw;
            }
        }

        /// <summary>
        /// 根据 ID 获取验方详情
        /// </summary>
        public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("[REPO] Formula.GetById started - Id={Id}", id);

                // 获取包含药材的完整验方
                var entity = await _dataSource.GetWithHerbsAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("[REPO] Formula.GetById -> NotFound - Id={Id}", id);
                    return null;
                }

                var dto = _mapper.ToDetailDto(entity);
                _logger.LogDebug("[REPO] Formula.GetById completed - Id={Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Formula.GetById failed - Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新验方
        /// </summary>
        public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] Formula.Create started - Name={Name}", dto.Name);

                var entity = _mapper.ToEntity(dto);
                var created = await _dataSource.CreateAsync(entity);
                var result = _mapper.ToDetailDto(created);

                _logger.LogInformation("[REPO] Formula.Create completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Formula.Create failed - Name={Name}", dto.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新验方信息
        /// </summary>
        public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.Id == null || dto.Id == Guid.Empty)
                throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] Formula.Update started - Id={Id}", dto.Id);

                var entity = _mapper.ToEntity(dto);
                var updated = await _dataSource.UpdateAsync(entity);
                var result = _mapper.ToDetailDto(updated);

                _logger.LogInformation("[REPO] Formula.Update completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Formula.Update failed - Id={Id}", dto.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[REPO] Formula.Delete started - Id={Id}", id);

                var result = await _dataSource.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("[REPO] Formula.Delete completed - Id={Id}", id);
                }
                else
                {
                    _logger.LogWarning("[REPO] Formula.Delete -> Failed - Id={Id}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Formula.Delete failed - Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 搜索验方（基于关键词，返回 ListDto）
        /// </summary>
        public async Task<List<FormulaListDto>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogDebug("[REPO] Formula.Search started - Keyword={Keyword}", keyword);

                var (items, _) = await _dataSource.GetPagedAsync(1, 100, keyword, null);

                var listDtos = items.Select(e => new FormulaListDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Effect = e.Effect,
                    Indications = e.Indication,
                    Category = e.Category,
                    IsShared = e.IsShared,
                    ValidationStatus = e.ValidationStatus,
                    Status = e.Status,
                    HerbCount = e.Herbs?.Count ?? 0,
                    TotalPrice = 0, // 需要从药材库获取价格，暂时设为0
                    CreatedAt = e.CreatedAt
                }).ToList();

                _logger.LogDebug("[REPO] Formula.Search completed - Count={Count}", listDtos.Count);
                return listDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Formula.Search failed - Keyword={Keyword}", keyword);
                throw;
            }
        }

        #endregion

        #region 验方专用方法

        /// <summary>
        /// 克隆验方
        /// </summary>
        public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("[REPO] Formula.Clone started - Id={Id}", formulaId);

                var cloned = await _dataSource.CloneAsync(formulaId);
                if (cloned == null)
                {
                    throw new InvalidOperationException($"克隆验方失败，ID: {formulaId}");
                }

                var dto = _mapper.ToDetailDto(cloned);
                _logger.LogInformation("[REPO] Formula.Clone completed - OriginalId={OriginalId} ClonedId={ClonedId}", formulaId, dto.Id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Formula.Clone failed - Id={Id}", formulaId);
                throw;
            }
        }

        // OpenSpec: cleanup-formula-dead-code - 已删除GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync

        #endregion

        #region 状态切换、恢复和批量操作

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("切换验方状态：{Id}", id);

                var result = await _dataSource.ToggleStatusAsync(id);
                if (!result)
                {
                    _logger.LogError("切换验方状态失败：{Id}", id);
                    return null;
                }

                // 重新获取更新后的数据
                var entity = await _dataSource.GetWithHerbsAsync(id);
                if (entity == null)
                {
                    return null;
                }

                var dto = _mapper.ToDetailDto(entity);
                _logger.LogInformation("验方状态已切换为：{Status}", dto.Status);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换验方状态时发生异常：{Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 恢复已删除的验方
        /// </summary>
        public async Task<FormulaDetailDto?> RestoreAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("恢复验方：{Id}", id);

                var entity = await _dataSource.RestoreAsync(id);
                if (entity == null)
                {
                    _logger.LogError("恢复验方失败：{Id}", id);
                    return null;
                }

                var dto = _mapper.ToDetailDto(entity);
                _logger.LogInformation("验方已恢复：{Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复验方时发生异常：{Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 批量删除验方
        /// 注意：仅 Remote 模式支持批量 API 操作
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                // 本地模式：逐个删除
                _logger.LogInformation("批量删除验方（本地模式）：{Count}个", ids.Count);
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

                _logger.LogInformation("批量删除验方完成：成功{Success}个，失败{Failure}个", successCount, failureCount);
                return new BatchOperationResultDto { SuccessCount = successCount, FailureCount = failureCount };
            }

            try
            {
                _logger.LogInformation("批量删除验方：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDeleteAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量删除验方失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量删除验方完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除验方时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 批量启用验方
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Formula.BatchEnable -> NotSupported - 本地模式不支持批量启用");
                return null;
            }

            try
            {
                _logger.LogInformation("批量启用验方：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchEnableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量启用验方失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量启用验方完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用验方时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 批量禁用验方
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Formula.BatchDisable -> NotSupported - 本地模式不支持批量禁用");
                return null;
            }

            try
            {
                _logger.LogInformation("批量禁用验方：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDisableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量禁用验方失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量禁用验方完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用验方时发生异常");
                return null;
            }
        }

        #endregion
    }
}
