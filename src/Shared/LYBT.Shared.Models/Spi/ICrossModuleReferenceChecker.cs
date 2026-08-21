namespace LYBT.Shared.Models.Spi;

/// <summary>SPI：跨模块引用检查扩展点。新增实体仅需实现此接口并注册，删除前统一经注册表校验，无需改动现有删除链。</summary>
public interface ICrossModuleReferenceChecker
{
    /// <summary>被检查实体名（如 Herb、Formula、Patient）。</summary>
    string EntityName { get; }

    /// <summary>是否存在跨模块引用。</summary>
    Task<bool> HasReferencesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>引用计数（用于提示文案）。</summary>
    Task<int> GetReferenceCountAsync(Guid id, CancellationToken cancellationToken = default);
}
