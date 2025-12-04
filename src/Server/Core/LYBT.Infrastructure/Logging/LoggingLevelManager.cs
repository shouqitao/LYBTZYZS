using Serilog.Core;
using Serilog.Events;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// 日志级别管理器
/// refactor-logging-system: 支持运行时动态调整日志级别
/// </summary>
public class LoggingLevelManager : IDisposable
{
    private bool _disposed;
    /// <summary>
    /// 日志级别开关 - 全局单例，允许运行时修改
    /// </summary>
    public LoggingLevelSwitch LevelSwitch { get; }

    /// <summary>
    /// 默认日志级别（用于恢复）
    /// </summary>
    public LogEventLevel DefaultLevel { get; }

    /// <summary>
    /// 当前是否处于调试模式
    /// </summary>
    public bool IsDebugModeActive => LevelSwitch.MinimumLevel < DefaultLevel;

    /// <summary>
    /// 调试模式开始时间（如果启用）
    /// </summary>
    public DateTime? DebugModeStartedAt { get; private set; }

    /// <summary>
    /// 调试模式自动过期时间（如果设置）
    /// </summary>
    public DateTime? DebugModeExpiresAt { get; private set; }

    private readonly object _lock = new();
    private Timer? _expirationTimer;

    public LoggingLevelManager(LogEventLevel defaultLevel = LogEventLevel.Information)
    {
        DefaultLevel = defaultLevel;
        LevelSwitch = new LoggingLevelSwitch(defaultLevel);
    }

    /// <summary>
    /// 启用调试模式（降低日志级别以捕获更多信息）
    /// </summary>
    /// <param name="level">目标日志级别（默认Debug）</param>
    /// <param name="durationMinutes">持续时间（分钟），null表示不自动过期</param>
    /// <returns>调试模式信息</returns>
    public DebugModeInfo EnableDebugMode(LogEventLevel level = LogEventLevel.Debug, int? durationMinutes = 30)
    {
        lock (_lock)
        {
            // 停止现有计时器
            _expirationTimer?.Dispose();
            _expirationTimer = null;

            // 设置新级别
            var previousLevel = LevelSwitch.MinimumLevel;
            LevelSwitch.MinimumLevel = level;
            DebugModeStartedAt = DateTime.UtcNow;

            if (durationMinutes.HasValue && durationMinutes > 0)
            {
                DebugModeExpiresAt = DebugModeStartedAt.Value.AddMinutes(durationMinutes.Value);

                // 设置自动过期计时器
                _expirationTimer = new Timer(
                    _ => DisableDebugMode(),
                    null,
                    TimeSpan.FromMinutes(durationMinutes.Value),
                    Timeout.InfiniteTimeSpan);
            }
            else
            {
                DebugModeExpiresAt = null;
            }

            return new DebugModeInfo
            {
                IsActive = true,
                PreviousLevel = previousLevel.ToString(),
                CurrentLevel = level.ToString(),
                StartedAt = DebugModeStartedAt.Value,
                ExpiresAt = DebugModeExpiresAt,
                DurationMinutes = durationMinutes
            };
        }
    }

    /// <summary>
    /// 禁用调试模式（恢复默认日志级别）
    /// </summary>
    /// <returns>调试模式信息</returns>
    public DebugModeInfo DisableDebugMode()
    {
        lock (_lock)
        {
            // 停止计时器
            _expirationTimer?.Dispose();
            _expirationTimer = null;

            var previousLevel = LevelSwitch.MinimumLevel;
            LevelSwitch.MinimumLevel = DefaultLevel;
            DebugModeStartedAt = null;
            DebugModeExpiresAt = null;

            return new DebugModeInfo
            {
                IsActive = false,
                PreviousLevel = previousLevel.ToString(),
                CurrentLevel = DefaultLevel.ToString(),
                StartedAt = null,
                ExpiresAt = null,
                DurationMinutes = null
            };
        }
    }

    /// <summary>
    /// 获取当前调试模式状态
    /// </summary>
    public DebugModeInfo GetStatus()
    {
        lock (_lock)
        {
            return new DebugModeInfo
            {
                IsActive = IsDebugModeActive,
                PreviousLevel = null,
                CurrentLevel = LevelSwitch.MinimumLevel.ToString(),
                StartedAt = DebugModeStartedAt,
                ExpiresAt = DebugModeExpiresAt,
                DurationMinutes = DebugModeExpiresAt.HasValue && DebugModeStartedAt.HasValue
                    ? (int?)(DebugModeExpiresAt.Value - DebugModeStartedAt.Value).TotalMinutes
                    : null,
                DefaultLevel = DefaultLevel.ToString()
            };
        }
    }

    /// <summary>
    /// 设置特定日志级别
    /// </summary>
    /// <param name="level">目标级别</param>
    public void SetLevel(LogEventLevel level)
    {
        lock (_lock)
        {
            LevelSwitch.MinimumLevel = level;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的实际实现
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _expirationTimer?.Dispose();
            _expirationTimer = null;
        }

        _disposed = true;
    }
}

/// <summary>
/// 调试模式信息DTO
/// </summary>
public class DebugModeInfo
{
    /// <summary>
    /// 调试模式是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 之前的日志级别
    /// </summary>
    public string? PreviousLevel { get; set; }

    /// <summary>
    /// 当前日志级别
    /// </summary>
    public string CurrentLevel { get; set; } = string.Empty;

    /// <summary>
    /// 默认日志级别
    /// </summary>
    public string? DefaultLevel { get; set; }

    /// <summary>
    /// 调试模式开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 调试模式过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 调试模式持续时间（分钟）
    /// </summary>
    public int? DurationMinutes { get; set; }
}
