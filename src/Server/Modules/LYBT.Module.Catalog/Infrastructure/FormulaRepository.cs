using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Catalog.Infrastructure;

/// <summary>
/// 验方仓储实现。封装验方数据访问逻辑。
/// </summary>
public class FormulaRepository : CatalogRepositoryBase<Formula>, IFormulaRepository
{
    public FormulaRepository(CatalogDbContext context, ILogger<FormulaRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public override async Task<Formula> UpdateAsync(Formula entity, CancellationToken cancellationToken = default)
    {
        // 2026-08-13 深挖（formula-deep-fix）: 显式子集合替换——不依赖 EF 对 item 跟踪状态的隐式判断。
        // 第 2 层根因实证（EF SQL 日志）: ReplaceHerbs 的新 item 曾被 EF 误标 Modified（非 Added）→
        // UPDATE WHERE 新 Id → 0 rows 并发异常。显式模式根治：旧 items 按 FormulaId 从库删除（RemoveRange——
        // EF 对未跟踪实体自动 Attach 再标 Deleted），新 items 显式 Add（强制 Added——即使 EF 误标 Modified 也无碍）。
        // 父行走 base 语义（Attached 只 SaveChanges——RowVersion 仅 WHERE 正确值；Detached 保留 Update()）——
        // 乐观并发保持（真并发仍抛 DbUpdateConcurrencyException）。
        var oldItems = await _context.FormulaHerbItems
            .Where(i => i.FormulaId == entity.Id)
            .ToListAsync(cancellationToken);
        _context.FormulaHerbItems.RemoveRange(oldItems);

        foreach (var herb in entity.Herbs)
        {
            if (_context.Entry(herb).State == EntityState.Detached)
                _context.FormulaHerbItems.Add(herb);
        }

        return await base.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<Formula?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // US-LOG-000 P0-1（2026-08-13）: override 查询异常记录（基类 catch 不覆盖 override）
            _logger.LogError(ex, "[REPO] Formula.GetById({Id}) 查询失败（Include Herbs）", id);
            throw;
        }
    }

    /// <inheritdoc/>
    /// A-31-C5-3 例外：Formula 特化保留（需 Include Herbs 导航属性，与基类模板不同）
    public override async Task<Formula?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Formulas
                .Include(f => f.Herbs)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // US-LOG-000 P0-1（2026-08-13）: 恢复操作查询异常记录
            _logger.LogError(ex, "[REPO] Formula.GetByIdIncludingDeleted({Id}) 查询失败（Include Herbs）", id);
            throw;
        }
    }

    /// <inheritdoc/>
    /// R-20: 列表页不 Include(Herbs)（避免子集合过度加载）；HerbCount 走 COUNT 投影。
    /// includeChildren=true（导出明细）时才 Include。
    public override async Task<PagedResult<Formula>> GetPagedAsync(
        int page, int pageSize, string? keyword, string? category,
        Guid? operatorId = null, bool isAdmin = false,
        CancellationToken cancellationToken = default, bool includeChildren = false)
    {
        var query = _context.Formulas
            .AsNoTracking()
            .Where(f => !f.IsDeleted)
            .AsQueryable();

        if (includeChildren)
            query = query.Include(f => f.Herbs);

        // P1 (US-FORM-001): Doctor 仅可见本人 + 共享验方（Admin/SuperAdmin 全量）
        if (!isAdmin && operatorId.HasValue)
        {
            query = query.Where(f => f.UserId == operatorId.Value || f.IsShared);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(kw) ||
                (f.Effect != null && f.Effect.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => f.Category != null && f.Category.Contains(category));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        List<Formula> items;
        if (includeChildren)
        {
            items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // 列表路径：COUNT 投影填充 HerbCount，不加载 Herbs 集合
            var rows = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new { Entity = f, HerbCount = f.Herbs.Count })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
                row.Entity.LoadedHerbCount = row.HerbCount;
            items = rows.Select(r => r.Entity).ToList();
        }

        return new PagedResult<Formula>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public override Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        => ExistsAsync(f => f.Name == name, excludeId, ct);

    /// <inheritdoc/>
    public async Task<List<Formula>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<Formula, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .Where(f => !f.IsDeleted)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }
}
