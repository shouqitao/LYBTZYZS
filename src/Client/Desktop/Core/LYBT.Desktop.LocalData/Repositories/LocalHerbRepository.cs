using System.IO;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Repositories;

/// <summary>
/// 药材仓储 - 本地模式实现 (SYNC-D02)
/// 通过 EF Core + LocalDbContext 直接访问 SQL Server LocalDB。
/// DI 工厂根据 IConnectionModeProvider 在本地模式下选择此实现。
/// </summary>
public sealed class LocalHerbRepository : IHerbRepository
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalHerbRepository> _logger;
    private readonly LocalHerbMapper _mapper = new();

    public LocalHerbRepository(
        LocalDbContext context,
        ILogger<LocalHerbRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Herb.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                page, pageSize, keyword, category);

            var query = _context.Herbs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(h =>
                    h.Name.Contains(keyword) ||
                    (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(h => h.Category == category);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(h => h.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var listDtos = items.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new HerbListDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    PinYinCode = dto.PinYinCode,
                    Category = dto.Category,
                    Origin = dto.Origin,
                    Spec = dto.Spec,
                    Unit = dto.Unit,
                    Price = dto.Price,
                    Status = dto.Status,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();

            return new PagedResult<HerbListDto>
            {
                Items = listDtos,
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.GetPaged failed");
            throw;
        }
    }

    public async Task<HerbDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Herb.GetById - Id={Id}", id);

            var entity = await _context.Herbs
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Herb.GetById - NotFound: {Id}", id);
                return null;
            }

            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<HerbDetailDto> CreateAsync(HerbInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Local] Herb.Create - Name={Name}", dto.Name);

            var entity = _mapper.ToEntity(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.Status = CommonStatus.Enabled;

            _context.Herbs.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Herb.Create completed - Id={Id}", entity.Id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.Create failed");
            throw;
        }
    }

    public async Task<HerbDetailDto> UpdateAsync(HerbInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var id = dto.Id ?? throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            _logger.LogInformation("[REPO:Local] Herb.Update - Id={Id}", id);

            var existing = await _context.Herbs.FindAsync(id)
                ?? throw new InvalidOperationException($"药材不存在: {id}");

            existing.Name = dto.Name;
            existing.PinYinCode = dto.PinYinCode;
            existing.Category = dto.Category;
            existing.Origin = dto.Origin;
            existing.Spec = dto.Spec;
            existing.Unit = dto.Unit;
            existing.Price = dto.Price;
            existing.CostPrice = dto.CostPrice ?? existing.CostPrice;
            existing.Effect = dto.Effect;
            existing.Usage = dto.Usage;
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Herb.Update completed - Id={Id}", id);
            return _mapper.ToDetailDto(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.Update failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Herb.Delete - Id={Id}", id);

            var entity = await _context.Herbs.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Herb.Delete - NotFound: {Id}", id);
                return false;
            }

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Herb.Delete completed - Id={Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<HerbListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Herb.Search - Keyword={Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
                return [];

            var entities = await _context.Herbs
                .AsNoTracking()
                .Where(h =>
                    h.Name.Contains(keyword) ||
                    (h.PinYinCode != null && h.PinYinCode.Contains(keyword)))
                .OrderBy(h => h.Name)
                .Take(100)
                .ToListAsync();

            return entities.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new HerbListDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    PinYinCode = dto.PinYinCode,
                    Category = dto.Category,
                    Origin = dto.Origin,
                    Spec = dto.Spec,
                    Unit = dto.Unit,
                    Price = dto.Price,
                    Status = dto.Status,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.Search failed");
            throw;
        }
    }

    #endregion

    #region 批量导入/导出 (本地模式不支持)

    public Task<HerbBatchImportResultDto?> BatchImportAsync(Stream fileStream, string fileName)
    {
        _logger.LogWarning("[REPO:Local] Herb.BatchImport - 本地模式不支持批量导入");
        return Task.FromResult<HerbBatchImportResultDto?>(null);
    }

    public Task<byte[]?> ExportTemplateAsync()
    {
        _logger.LogWarning("[REPO:Local] Herb.ExportTemplate - 本地模式不支持导出模板");
        return Task.FromResult<byte[]?>(null);
    }

    public Task<byte[]?> ExportHerbsAsync(string? keyword = null)
    {
        _logger.LogWarning("[REPO:Local] Herb.ExportHerbs - 本地模式不支持导出");
        return Task.FromResult<byte[]?>(null);
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<HerbDetailDto?> ToggleStatusAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Herb.ToggleStatus - Id={Id}", id);

            var entity = await _context.Herbs.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Herb.ToggleStatus - NotFound: {Id}", id);
                return null;
            }

            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Herb.ToggleStatus completed - Status={Status}", entity.Status);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<HerbDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Herb.Restore - Id={Id}", id);

            var entity = await _context.Herbs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Herb.Restore - NotFound: {Id}", id);
                return null;
            }

            entity.IsDeleted = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Herb.Restore completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Herb.BatchDelete - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                IsSuccess = true
            };

            foreach (var id in ids)
            {
                var entity = await _context.Herbs.FindAsync(id);
                if (entity != null)
                {
                    entity.IsDeleted = true;
                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                }
                else
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "药材不存在"
                    });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Herb.BatchDelete completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.BatchDelete failed");
            return null;
        }
    }

    public Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        _logger.LogWarning("[REPO:Local] Herb.BatchEnable - 本地模式不支持批量启用");
        return Task.FromResult<BatchOperationResultDto?>(null);
    }

    public Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        _logger.LogWarning("[REPO:Local] Herb.BatchDisable - 本地模式不支持批量禁用");
        return Task.FromResult<BatchOperationResultDto?>(null);
    }

    #endregion

    #region 包装方法 (统一返回元组格式)

    public async Task<(bool success, HerbDetailDto? data, string? error)> CreateWithResultAsync(HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Herb.CreateWithResult - Name={Name}", input.Name);
            var result = await CreateAsync(input);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.CreateWithResult failed - Name={Name}", input.Name);
            return (false, null, $"创建中药失败: {ex.Message}");
        }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> UpdateWithResultAsync(Guid id, HerbInputDto input)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Herb.UpdateWithResult - Id={Id}", id);
            var result = await UpdateAsync(input);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.UpdateWithResult failed - Id={Id}", id);
            return (false, null, $"更新中药失败: {ex.Message}");
        }
    }

    public async Task<(bool success, string? error)> DeleteWithResultAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Herb.DeleteWithResult - Id={Id}", id);
            var result = await DeleteAsync(id);

            if (result)
                return (true, null);
            else
                return (false, "删除中药失败，记录不存在或已被删除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.DeleteWithResult failed - Id={Id}", id);
            return (false, $"删除中药失败: {ex.Message}");
        }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> GetByIdWithResultAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Herb.GetByIdWithResult - Id={Id}", id);
            var result = await GetByIdAsync(id);

            if (result != null)
                return (true, result, null);
            else
                return (false, null, "未找到指定的中药记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Herb.GetByIdWithResult failed - Id={Id}", id);
            return (false, null, $"获取中药详情失败: {ex.Message}");
        }
    }

    #endregion
}
