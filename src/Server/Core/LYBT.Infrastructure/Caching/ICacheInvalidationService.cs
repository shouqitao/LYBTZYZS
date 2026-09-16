namespace LYBT.Infrastructure.Caching;

/// <summary>
/// 缓存失效服务接口 -- MemoryCache 前缀清理（P-01: OutputCache 已移除，仅 IMemoryCache 生效）
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// 按 tag 使对应的 MemoryCache 条目失效
    /// </summary>
    /// <param name="tag">缓存 tag (如 "herbs", "formulas", "patients", "medicalcases")</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task InvalidateAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按多个 tag 批量失效
    /// </summary>
    Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}


