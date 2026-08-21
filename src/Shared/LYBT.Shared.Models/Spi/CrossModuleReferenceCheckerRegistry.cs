namespace LYBT.Shared.Models.Spi;

/// <summary>跨模块引用检查 SPI 注册表 — 聚合所有已注册的 <see cref="ICrossModuleReferenceChecker"/>，删除前统一遍历。</summary>
public sealed class CrossModuleReferenceCheckerRegistry
{
    private readonly IReadOnlyList<ICrossModuleReferenceChecker> _checkers;

    public CrossModuleReferenceCheckerRegistry(IEnumerable<ICrossModuleReferenceChecker> checkers) => _checkers = checkers.ToList();

    public IReadOnlyList<ICrossModuleReferenceChecker> All => _checkers;

    /// <summary>汇总指定 ID 的全部引用计数（按 checker 逐项累加）。</summary>
    public async Task<int> GetTotalReferenceCountAsync(Guid id, CancellationToken ct = default)
    {
        var total = 0;
        foreach (var c in _checkers)
            total += await c.GetReferenceCountAsync(id, ct);
        return total;
    }

    /// <summary>是否存在任意引用。</summary>
    public async Task<bool> HasAnyReferenceAsync(Guid id, CancellationToken ct = default)
    {
        foreach (var c in _checkers)
            if (await c.HasReferencesAsync(id, ct))
                return true;
        return false;
    }
}
