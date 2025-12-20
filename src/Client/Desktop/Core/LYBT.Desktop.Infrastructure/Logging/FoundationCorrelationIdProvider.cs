using LYBT.Desktop.Foundation.Logging;
using LYBT.Shared.Logging.Abstractions;

namespace LYBT.Desktop.Infrastructure.Logging;

/// <summary>
/// 基于Foundation层CorrelationIdContext的CorrelationId提供者
/// 适配ICorrelationIdProvider接口，桥接Foundation层和Shared.Logging层
/// </summary>
public class FoundationCorrelationIdProvider : ICorrelationIdProvider
{
    /// <summary>
    /// 从CorrelationIdContext获取当前CorrelationId
    /// </summary>
    /// <returns>当前CorrelationId，如果不存在返回null</returns>
    public string? GetCorrelationId()
    {
        return CorrelationIdContext.Current;
    }

    /// <summary>
    /// 设置CorrelationId到CorrelationIdContext
    /// </summary>
    /// <param name="correlationId">要设置的CorrelationId</param>
    public void SetCorrelationId(string correlationId)
    {
        CorrelationIdContext.Current = correlationId;
    }
}
