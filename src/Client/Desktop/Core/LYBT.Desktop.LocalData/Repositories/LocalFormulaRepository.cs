using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Repositories;

/// <summary>
/// 验方仓储 - 本地模式实现 (SYNC-D02)
/// 通过 EF Core + LocalDbContext 直接访问本地数据库。
/// DI 工厂根据 IConnectionModeProvider 在本地模式下选择此实现。
/// </summary>
public sealed class LocalFormulaRepository : IFormulaRepository
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalFormulaRepository> _logger;
    private readonly LocalFormulaMapper _mapper = new();

    public LocalFormulaRepository(
        LocalDbContext context,
        ILogger<LocalFormulaRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Formula.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                page, pageSize, keyword, category);

            var query = _context.Formulas.AsNoTracking();

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(f =>
                    f.Name.Contains(keyword) ||
                    (f.Effect != null && f.Effect.Contains(keyword)) ||
                    (f.Indication != null && f.Indication.Contains(keyword)));
            }

            // 分类过滤
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(f => f.Category == category);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var listDtos = items.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new FormulaListDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Effect = dto.Effect,
                    Indications = dto.Indications,
                    Category = dto.Category,
                    IsShared = dto.IsShared,
                    ValidationStatus = dto.ValidationStatus,
                    Status = dto.Status,
                    HerbCount = dto.HerbCount,
                    TotalPrice = 0,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();

            return new PagedResult<FormulaListDto>
            {
                Items = listDtos,
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.GetPaged failed");
            throw;
        }
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Formula.GetById - Id={Id}", id);

            // 获取包含药材子项的完整验方
            var entity = await _context.Formulas
                .AsNoTracking()
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Formula.GetById - NotFound: {Id}", id);
                return null;
            }

            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Local] Formula.Create - Name={Name}", dto.Name);

            var entity = _mapper.ToEntity(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.Status = CommonStatus.Enabled;
            entity.ValidationStatus = FormulaValidationStatus.Draft;

            // 为药材项设置 ID 和关联
            foreach (var herb in entity.Herbs)
            {
                herb.Id = Guid.NewGuid();
                herb.FormulaId = entity.Id;
            }

            _context.Formulas.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.Create completed - Id={Id}", entity.Id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.Create failed");
            throw;
        }
    }

    public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var id = dto.Id ?? throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            _logger.LogInformation("[REPO:Local] Formula.Update - Id={Id}", id);

            var existing = await _context.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id)
                ?? throw new InvalidOperationException($"验方不存在: {id}");

            // 更新基本属性
            existing.Name = dto.Name;
            existing.Effect = dto.Effect;
            existing.Usage = dto.Usage;
            existing.Property = dto.Property;
            existing.Category = dto.Category;
            existing.IsShared = dto.IsShared;
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;

            // 更新药材项 (删除旧的，添加新的)
            // 注意: 必须先 ToList() 避免迭代时修改集合
            _context.FormulaHerbItems.RemoveRange(existing.Herbs.ToList());

            foreach (var herbInput in dto.Herbs)
            {
                var herbEntity = _mapper.ToEntity(herbInput);
                herbEntity.Id = Guid.NewGuid();
                herbEntity.FormulaId = id;
                _context.FormulaHerbItems.Add(herbEntity);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.Update completed - Id={Id}", id);
            return _mapper.ToDetailDto(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.Update failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Formula.Delete - Id={Id}", id);

            var entity = await _context.Formulas.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Formula.Delete - NotFound: {Id}", id);
                return false;
            }

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.Delete completed - Id={Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<FormulaListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Formula.Search - Keyword={Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
                return [];

            var entities = await _context.Formulas
                .AsNoTracking()
                .Where(f =>
                    f.Name.Contains(keyword) ||
                    (f.Effect != null && f.Effect.Contains(keyword)) ||
                    (f.Indication != null && f.Indication.Contains(keyword)))
                .OrderByDescending(f => f.CreatedAt)
                .Take(100)
                .ToListAsync();

            return entities.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new FormulaListDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Effect = dto.Effect,
                    Indications = dto.Indications,
                    Category = dto.Category,
                    IsShared = dto.IsShared,
                    ValidationStatus = dto.ValidationStatus,
                    Status = dto.Status,
                    HerbCount = dto.HerbCount,
                    TotalPrice = 0,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.Search failed");
            throw;
        }
    }

    #endregion

    #region 验方专用方法

    public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Formula.Clone - Id={Id}", formulaId);

            var source = await _context.Formulas
                .AsNoTracking()
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == formulaId)
                ?? throw new InvalidOperationException($"克隆验方失败，原始验方不存在: {formulaId}");

            // 创建副本
            var clone = new Formula
            {
                Id = Guid.NewGuid(),
                Name = $"{source.Name} (副本)",
                Effect = source.Effect,
                Indication = source.Indication,
                Usage = source.Usage,
                Remark = source.Remark,
                Property = source.Property,
                Status = CommonStatus.Enabled,
                IsShared = false,
                ValidationStatus = FormulaValidationStatus.Draft,
                Category = source.Category,
                FormulaType = source.FormulaType,
                UserId = source.UserId
            };

            // 复制药材项
            foreach (var herb in source.Herbs)
            {
                clone.Herbs.Add(new FormulaHerbItem
                {
                    Id = Guid.NewGuid(),
                    FormulaId = clone.Id,
                    HerbId = herb.HerbId,
                    HerbName = herb.HerbName,
                    OriginalHerbName = herb.OriginalHerbName,
                    IsValidated = herb.IsValidated,
                    Dosage = herb.Dosage,
                    Unit = herb.Unit,
                    Usage = herb.Usage,
                    Remark = herb.Remark,
                    ProcessingMethod = herb.ProcessingMethod,
                    DecocteMethod = herb.DecocteMethod
                });
            }

            _context.Formulas.Add(clone);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.Clone completed - OriginalId={OriginalId} ClonedId={ClonedId}",
                formulaId, clone.Id);
            return _mapper.ToDetailDto(clone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.Clone failed - Id={Id}", formulaId);
            throw;
        }
    }

    // OpenSpec: cleanup-formula-dead-code - 已删除 GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Formula.ToggleStatus - Id={Id}", id);

            var entity = await _context.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Formula.ToggleStatus - NotFound: {Id}", id);
                return null;
            }

            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.ToggleStatus completed - Status={Status}", entity.Status);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Formula.Restore - Id={Id}", id);

            var entity = await _context.Formulas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Formula.Restore - NotFound: {Id}", id);
                return null;
            }

            entity.IsDeleted = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.Restore completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Formula.BatchDelete - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                IsSuccess = true
            };

            foreach (var id in ids)
            {
                var entity = await _context.Formulas.FindAsync(id);
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
                        Reason = "验方不存在"
                    });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.BatchDelete completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.BatchDelete failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Formula.BatchEnable - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                IsSuccess = true
            };

            foreach (var id in ids)
            {
                var entity = await _context.Formulas.FindAsync(id);
                if (entity != null)
                {
                    entity.Status = CommonStatus.Enabled;
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
                        Reason = "验方不存在"
                    });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.BatchEnable completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.BatchEnable failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Formula.BatchDisable - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                IsSuccess = true
            };

            foreach (var id in ids)
            {
                var entity = await _context.Formulas.FindAsync(id);
                if (entity != null)
                {
                    entity.Status = CommonStatus.Disabled;
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
                        Reason = "验方不存在"
                    });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Formula.BatchDisable completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Formula.BatchDisable failed");
            return null;
        }
    }

    #endregion
}
