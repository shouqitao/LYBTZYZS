namespace LYBT.Shared.Logging.Abstractions;

/// <summary>
/// CorrelationId提供者接口
/// 用于解耦HttpContext依赖，支持Server和Desktop不同实现
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// 获取当前CorrelationId
    /// </summary>
    /// <returns>当前请求/操作的CorrelationId，如果不存在返回null</returns>
    string? GetCorrelationId();

    /// <summary>
    /// 设置当前CorrelationId
    /// </summary>
    /// <param name="correlationId">要设置的CorrelationId</param>
    void SetCorrelationId(string correlationId);
}
