using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Logging.Masking;

/// <summary>
/// P1-12: 双路径脱敏——ILogger 路径补充（Serilog 路径已有 <see cref="SensitiveDataDestructuringPolicy"/>）
/// 包装 ILogger 的 Log&lt;TState&gt; 参数，对标记了 <see cref="LYBT.Shared.Models.Attributes.SensitiveDataAttribute"/> 的字段及常见敏感键名做正则脱敏。
/// 仅在文本脱敏发生变更时写入（避免与已注册的 Serilog provider 双重输出）；未脱敏消息由宿主的正常 provider 链处理。
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

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var sanitized = SensitiveDataMasker.SanitizeText(message);

            // 仅在脱敏实际改变内容时写入，避免与宿主已注册的 Serilog provider 双重输出
            if (string.Equals(message, sanitized, StringComparison.Ordinal))
                return;

            Serilog.Log.Write(MapLevel(logLevel), exception, "[sanitized:{Category}] {Message}", _category, sanitized);
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

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
