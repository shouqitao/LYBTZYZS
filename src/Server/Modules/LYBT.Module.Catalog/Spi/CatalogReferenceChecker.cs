using LYBT.Shared.Models.Spi;

namespace LYBT.Module.Catalog.Spi;

/// <summary>示例库存引用检查 — 演示新增库存实体仅需新增类 + DI 注册，删除前由注册表统一校验。</summary>
public sealed class CatalogReferenceChecker : ICrossModuleReferenceChecker
{
    public string EntityName => "Herb";

    // 示例存根：新增实体可注入仓储实现真实引用计数；此存根展示扩展点无需改现有删除链。
    public Task<bool> HasReferencesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<int> GetReferenceCountAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(0);
}
