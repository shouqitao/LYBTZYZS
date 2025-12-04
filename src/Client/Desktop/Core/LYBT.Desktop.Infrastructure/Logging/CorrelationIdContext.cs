using FoundationCorrelationIdContext = LYBT.Desktop.Foundation.Logging.CorrelationIdContext;

namespace LYBT.Desktop.Infrastructure.Logging;

/// <summary>
/// Infrastructure层CorrelationId上下文
/// refactor-logging-system: 包装Foundation层的基础实现，保持API兼容
/// </summary>
public static class CorrelationIdContext
{
    /// <summary>
    /// 获取或设置当前CorrelationId
    /// </summary>
    public static string? Current
    {
        get => FoundationCorrelationIdContext.Current;
        set => FoundationCorrelationIdContext.Current = value;
    }

    /// <summary>
    /// 获取当前CorrelationId，如果为空则生成新的
    /// </summary>
    public static string CurrentOrNew => FoundationCorrelationIdContext.CurrentOrNew;

    /// <summary>
    /// 生成新的CorrelationId并设置为当前值
    /// </summary>
    public static string GenerateNew() => FoundationCorrelationIdContext.GenerateNew();

    /// <summary>
    /// 创建一个新的CorrelationId作用域
    /// </summary>
    public static IDisposable BeginScope(string? correlationId = null)
        => FoundationCorrelationIdContext.BeginScope(correlationId);

    /// <summary>
    /// 清除当前CorrelationId
    /// </summary>
    public static void Clear() => FoundationCorrelationIdContext.Clear();
}
