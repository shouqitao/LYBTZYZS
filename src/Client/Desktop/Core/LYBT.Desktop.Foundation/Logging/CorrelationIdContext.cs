namespace LYBT.Desktop.Foundation.Logging;

/// <summary>
/// CorrelationId上下文管理（基础实现）
/// refactor-logging-system: 使用AsyncLocal存储CorrelationId，支持跨异步调用传递
/// Foundation层基础实现，不依赖Serilog
/// </summary>
public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// 获取或设置当前CorrelationId
    /// </summary>
    public static string? Current
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    /// <summary>
    /// 获取当前CorrelationId，如果为空则生成新的
    /// </summary>
    public static string CurrentOrNew => Current ?? GenerateNew();

    /// <summary>
    /// 生成新的CorrelationId并设置为当前值
    /// </summary>
    /// <returns>新生成的CorrelationId</returns>
    public static string GenerateNew()
    {
        var correlationId = GenerateCorrelationId();
        Current = correlationId;
        return correlationId;
    }

    /// <summary>
    /// 创建一个新的CorrelationId作用域
    /// 作用域结束时自动恢复之前的CorrelationId
    /// </summary>
    /// <param name="correlationId">可选的CorrelationId，为空时自动生成</param>
    /// <returns>可释放的作用域对象</returns>
    public static IDisposable BeginScope(string? correlationId = null)
    {
        return new CorrelationIdScope(correlationId ?? GenerateCorrelationId());
    }

    /// <summary>
    /// 清除当前CorrelationId
    /// </summary>
    public static void Clear()
    {
        Current = null;
    }

    /// <summary>
    /// 生成CorrelationId
    /// 格式: LYBT-yyyyMMddHHmmssfff-XXXX
    /// </summary>
    private static string GenerateCorrelationId()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var random = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"LYBT-{timestamp}-{random}";
    }

    /// <summary>
    /// CorrelationId作用域
    /// </summary>
    private sealed class CorrelationIdScope : IDisposable
    {
        private readonly string? _previousCorrelationId;
        private bool _disposed;

        public CorrelationIdScope(string correlationId)
        {
            _previousCorrelationId = Current;
            Current = correlationId;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Current = _previousCorrelationId;
        }
    }
}
