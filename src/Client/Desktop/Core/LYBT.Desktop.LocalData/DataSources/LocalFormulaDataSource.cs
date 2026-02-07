using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Formulas;
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

    public LocalFormulaDataSource(LocalDbContext context, ILogger<LocalFormulaDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Formula?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.GetById - Id={Id}", id);
        return await _context.Formulas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<Formula?> GetWithHerbsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Formula.GetWithHerbs - Id={Id}", id);
        return await _context.Formulas
            .AsNoTracking()
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<(List<Formula> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        return await GetPagedAsync(page, pageSize, keyword, null, ct);
    }

    public async Task<(List<Formula> Items, int Total)> GetPagedAsync(
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

        return (items, total);
    }

    public async Task<Formula> CreateAsync(Formula entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.Create - Name={Name}", entity.Name);

        entity.Id = Guid.NewGuid();

        // 为药材项设置ID和关联
        foreach (var herb in entity.Herbs)
        {
            herb.Id = Guid.NewGuid();
            herb.FormulaId = entity.Id;
        }

        _context.Formulas.Add(entity);
        await _context.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<Formula> UpdateAsync(Formula entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.Update - Id={Id}", entity.Id);

        var existing = await _context.Formulas
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == entity.Id, ct)
            ?? throw new InvalidOperationException($"验方不存在: {entity.Id}");

        // 更新基本属性
        _context.Entry(existing).CurrentValues.SetValues(entity);

        // 更新药材项（删除旧的，添加新的）
        // 注意：必须先 ToList() 避免迭代时修改集合
        _context.FormulaHerbItems.RemoveRange(existing.Herbs.ToList());

        foreach (var herb in entity.Herbs)
        {
            herb.Id = Guid.NewGuid();
            herb.FormulaId = entity.Id;
            _context.FormulaHerbItems.Add(herb);
        }

        await _context.SaveChangesAsync(ct);

        return existing;
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

    public async Task<Formula?> CloneAsync(Guid id, CancellationToken ct = default)
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
        return clone;
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

    public async Task<Formula?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Formula.Restore - Id={Id}", id);

        var entity = await _context.Formulas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (entity == null)
            return null;

        entity.IsDeleted = false;
        await _context.SaveChangesAsync(ct);
        return entity;
    }
}
