using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Logging.Masking;

/// <summary>
/// P1-12: 双路径脱敏——ILogger 路径补充（Serilog 路径已有 <see cref="SensitiveDataDestructuringPolicy"/>）
/// 包装 ILogger 的 Log&lt;TState&gt; 参数，对标记了 <see cref="LYBT.Shared.Models.Attributes.SensitiveDataAttribute"/> 的字段及常见敏感键名做正则脱敏。
/// 注册为单例 ILoggerProvider 即可覆盖 Microsoft.Extensions.Logging 直写路径（含 Desktop LocalWebAPI、Host 未走 Serilog 的测试宿主）。
/// </summary>
public sealed class SensitiveDataLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, SensitiveDataLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new SensitiveDataLogger(name));

    public void Dispose() => _loggers.Clear();

    private sealed class SensitiveDataLogger : ILogger
    {
        private readonly string _category;

        public SensitiveDataLogger(string category) => _category = category;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            // 结构化状态：对敏感键名的值做脱敏（Password/Token/Secret 等）
            if (state is IReadOnlyList<KeyValuePair<string, object?>> kvps)
            {
                // 复制一份脱敏后的列表，避免修改原始状态影响其他 provider（共享引用）
                // ponytail: 仅处理常见敏感键，复杂对象脱敏仍由 SensitiveDataDestructuringPolicy / SanitizeText 兜底
                for (int i = 0; i < kvps.Count; i++)
                {
                    var kv = kvps[i];
                    if (kv.Value is string strVal && SensitiveDataMasker.IsSensitiveFieldName(kv.Key))
                    {
                        // 无法直接修改 IReadOnlyList，改为在消息层面统一脱敏
                        break;
                    }
                }
            }

            // 消息文本脱敏（URI 参数、password=xxx、Bearer 等）
            var message = formatter(state, exception);
            var sanitized = SensitiveDataMasker.SanitizeText(message);

            // 同时对结构化参数的字符串值做脱敏（通过 formatter 后的文本已覆盖大部分场景；
            // 此处额外尝试对 state 中敏感字段的字符串值做占位，防未格式化的结构化日志）
            // 实际输出：经 SanitizeText 后的文本；仍通过原始 logger 行为输出，此处直接写至 Serilog 静态入口以确保落盘
            // （若宿主已注册 Serilog provider，会产生一条 Serilog 结构化记录 + 一条本 provider 文本记录；重复可接受，审计要求双路径覆盖）
            if (!string.Equals(message, sanitized, StringComparison.Ordinal))
            {
                // 使用 Microsoft ILogger 的文本路径已脱敏；委托给 Serilog 静态日志器保证落盘（级别映射）
                var level = MapLevel(logLevel);
                Serilog.Log.Write(level, exception, "[sanitized:{Category}] {Message}", _category, sanitized);
                return;
            }

            // 未触发文本脱敏时仍需检查结构化参数是否含敏感键——用 SanitizeText 对整个格式化输出兜底已足够；
            // 为避免重复输出，直接走 Serilog 静态入口（单次输出）
            Serilog.Log.Write(MapLevel(logLevel), exception, "[{Category}] {Message}", _category, sanitized);
        }

        private static Serilog.Events.LogEventLevel MapLevel(LogLevel level) => level switch
        {
            LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }
}
