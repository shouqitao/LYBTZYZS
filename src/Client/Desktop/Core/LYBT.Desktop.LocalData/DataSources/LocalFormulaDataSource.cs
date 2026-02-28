using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.DataSources;

/// <summary>
/// 本地验方数据源实现 - SQLite EF Core
/// OpenSpec: implement-local-mode
/// </summary>
public class LocalFormulaDataSource : IFormulaDataSource
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalFormulaDataSource> _logger;
    private readonly LocalFormulaMapper _mapper = new();

    public LocalFormulaDataSource(LocalDbContext context, ILogger<LocalFormulaDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.GetById - Id={Id}", id);
        var entity = await _context.Formulas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<FormulaDetailDto?> GetWithHerbsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.GetWithHerbs - Id={Id}", id);
        var entity = await _context.Formulas
            .AsNoTracking()
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<(List<FormulaDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        return await GetPagedAsync(page, pageSize, keyword, null, ct);
    }

    public async Task<(List<FormulaDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? category,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.GetPaged - Page={Page}, Keyword={Keyword}, Category={Category}",
            page, keyword, category);

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

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.Create - Name={Name}", input.Name);

        var entity = _mapper.ToEntity(input);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        entity.Status = CommonStatus.Enabled;
        entity.ValidationStatus = FormulaValidationStatus.Draft;

        // 为药材项设置ID和关联
        foreach (var herb in entity.Herbs)
        {
            herb.Id = Guid.NewGuid();
            herb.FormulaId = entity.Id;
        }

        _context.Formulas.Add(entity);
        await _context.SaveChangesAsync(ct);

        return _mapper.ToDetailDto(entity);
    }

    public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto input, CancellationToken ct = default)
    {
        var id = input.Id ?? throw new InvalidOperationException("更新验方时必须提供ID");
        _logger.LogInformation("[LocalDataSource] Formula.Update - Id={Id}", id);

        var existing = await _context.Formulas
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new InvalidOperationException($"验方不存在: {id}");

        // 更新基本属性
        existing.Name = input.Name;
        existing.Effect = input.Effect;
        existing.Usage = input.Usage;
        existing.Property = input.Property;
        existing.Category = input.Category;
        existing.IsShared = input.IsShared;
        existing.Remark = input.Remark;
        existing.UpdatedAt = DateTime.Now;

        // 更新药材项（删除旧的，添加新的）
        // 注意：必须先 ToList() 避免迭代时修改集合
        _context.FormulaHerbItems.RemoveRange(existing.Herbs.ToList());

        foreach (var herbInput in input.Herbs)
        {
            var herbEntity = _mapper.ToEntity(herbInput);
            herbEntity.Id = Guid.NewGuid();
            herbEntity.FormulaId = id;
            _context.FormulaHerbItems.Add(herbEntity);
        }

        await _context.SaveChangesAsync(ct);

        return _mapper.ToDetailDto(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.Delete - Id={Id}", id);

        var entity = await _context.Formulas.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<FormulaDetailDto?> CloneAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.Clone - Id={Id}", id);

        var source = await _context.Formulas
            .AsNoTracking()
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (source == null)
            return null;

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
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Formula.Clone completed - NewId={NewId}", clone.Id);
        return _mapper.ToDetailDto(clone);
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.ToggleStatus - Id={Id}", id);

        var entity = await _context.Formulas.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.Status = entity.Status == CommonStatus.Enabled
            ? CommonStatus.Disabled
            : CommonStatus.Enabled;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.Restore - Id={Id}", id);

        var entity = await _context.Formulas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (entity == null)
            return null;

        entity.IsDeleted = false;
        await _context.SaveChangesAsync(ct);
        return _mapper.ToDetailDto(entity);
    }

    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <inheritdoc />
    public async Task<BatchOperationResultDto> BatchImportAsync(List<FormulaImportItemDto> items, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.BatchImport - Count={Count}", items.Count);

        var result = new BatchOperationResultDto
        {
            TotalCount = items.Count,
            IsSuccess = true
        };

        foreach (var item in items)
        {
            try
            {
                var entity = new Formula
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    Effect = item.Effect,
                    Usage = item.Usage,
                    Property = item.Property,
                    IsShared = item.IsShared,
                    Remark = item.Remark,
                    Status = CommonStatus.Enabled,
                    ValidationStatus = FormulaValidationStatus.Draft,
                    CreatedAt = DateTime.Now
                };

                // 创建药材项（延迟绑定模式）
                foreach (var herbImport in item.Herbs)
                {
                    entity.Herbs.Add(new FormulaHerbItem
                    {
                        Id = Guid.NewGuid(),
                        FormulaId = entity.Id,
                        HerbName = herbImport.HerbName,
                        OriginalHerbName = herbImport.HerbName,
                        Dosage = herbImport.Dosage,
                        Unit = herbImport.Unit ?? "g",
                        IsValidated = false
                    });
                }

                _context.Formulas.Add(entity);
                await _context.SaveChangesAsync(ct);

                result.SuccessCount++;
                result.SuccessfulIds.Add(entity.Id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(new BatchOperationFailureItem
                {
                    Reason = $"导入验方 '{item.Name}' 失败: {ex.Message}"
                });
                _logger.LogWarning(ex, "[LocalDataSource] Formula.BatchImport - Failed to import: {Name}", item.Name);
            }
        }

        result.IsSuccess = result.FailureCount == 0;
        _logger.LogInformation("[LocalDataSource] Formula.BatchImport completed - Success={Success}, Failure={Failure}",
            result.SuccessCount, result.FailureCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<List<FormulaDetailDto>> GetPendingValidationAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.GetPendingValidation");

        var entities = await _context.Formulas
            .AsNoTracking()
            .Include(f => f.Herbs)
            .Where(f => f.ValidationStatus == FormulaValidationStatus.Draft)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(e => _mapper.ToDetailDto(e)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<FormulaDetailDto>> GetAllForExportAsync(string? keyword = null, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.GetAllForExport - Keyword={Keyword}", keyword);

        var query = _context.Formulas
            .AsNoTracking()
            .Include(f => f.Herbs)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(f =>
                f.Name.Contains(keyword) ||
                (f.Effect != null && f.Effect.Contains(keyword)) ||
                (f.Indication != null && f.Indication.Contains(keyword)));
        }

        var entities = await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(e => _mapper.ToDetailDto(e)).ToList();
    }

    /// <summary>
    /// T5-P2-37: 批量启用/禁用验方
    /// </summary>
    public async Task<BatchOperationResultDto> BatchToggleStatusAsync(List<Guid> ids, bool enable, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.BatchToggleStatus - Count={Count}, Enable={Enable}", ids.Count, enable);

        var result = new BatchOperationResultDto
        {
            TotalCount = ids.Count,
            IsSuccess = true
        };

        var targetStatus = enable ? CommonStatus.Enabled : CommonStatus.Disabled;

        foreach (var id in ids)
        {
            var entity = await _context.Formulas.FindAsync([id], ct);
            if (entity != null)
            {
                entity.Status = targetStatus;
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

        await _context.SaveChangesAsync(ct);

        result.IsSuccess = result.FailureCount == 0;
        _logger.LogInformation("[LocalDataSource] Formula.BatchToggleStatus completed - Success={Success}, Failure={Failure}",
            result.SuccessCount, result.FailureCount);

        return result;
    }

    /// <summary>
    /// T5-P2-38: 获取导入模板列定义（主表）
    /// </summary>
    public string[] GetImportTemplateColumns()
        => ["验方编号*", "验方名称*", "分类", "功效", "用法", "性味归经", "方剂类型", "是否共享", "备注"];

    /// <summary>
    /// T5-P2-38: 获取导入模板列定义（药材明细）
    /// </summary>
    public string[] GetImportTemplateHerbColumns()
        => ["验方编号*", "药材名称*", "用量*", "单位", "炮制方法"];

    /// <inheritdoc />
    public async Task<bool> ValidateHerbBindingsAsync(Guid formulaId, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.ValidateHerbBindings - FormulaId={FormulaId}", formulaId);

        var herbItems = await _context.FormulaHerbItems
            .AsNoTracking()
            .Where(fhi => fhi.FormulaId == formulaId)
            .ToListAsync(ct);

        if (herbItems.Count == 0)
            return false;

        // 检查所有药材项是否都有有效的 HerbId 绑定
        foreach (var item in herbItems)
        {
            if (!item.HerbId.HasValue)
                return false;

            var herbExists = await _context.Herbs
                .AnyAsync(h => h.Id == item.HerbId.Value, ct);

            if (!herbExists)
                return false;
        }

        return true;
    }
}
