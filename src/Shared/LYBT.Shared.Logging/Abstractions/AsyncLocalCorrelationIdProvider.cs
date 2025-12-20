namespace LYBT.Shared.Logging.Abstractions;

/// <summary>
/// 基于AsyncLocal的CorrelationId提供者
/// 适用于Desktop客户端，使用AsyncLocal在异步上下文中传递CorrelationId
/// </summary>
public class AsyncLocalCorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// 默认CorrelationId值（当无法获取时使用）
    /// </summary>
    public const string DefaultCorrelationId = "N/A";

    /// <inheritdoc/>
    public string? GetCorrelationId()
    {
        return _correlationId.Value;
    }

    /// <inheritdoc/>
    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }

    /// <summary>
    /// 获取当前CorrelationId，如果不存在则返回默认值
    /// </summary>
    /// <returns>CorrelationId或默认值</returns>
    public string GetCorrelationIdOrDefault()
    {
        return _correlationId.Value ?? DefaultCorrelationId;
    }

    /// <summary>
    /// 获取当前CorrelationId，如果不存在则生成新的并设置
    /// </summary>
    /// <returns>当前或新生成的CorrelationId</returns>
    public string GetOrNew()
    {
        return _correlationId.Value ?? GenerateAndSet();
    }

    /// <summary>
    /// 清除当前CorrelationId
    /// </summary>
    public void Clear()
    {
        _correlationId.Value = null;
    }

    /// <summary>
    /// 生成新的CorrelationId并设置为当前值
    /// </summary>
    /// <returns>生成的CorrelationId</returns>
    public string GenerateAndSet()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        _correlationId.Value = correlationId;
        return correlationId;
    }
}
