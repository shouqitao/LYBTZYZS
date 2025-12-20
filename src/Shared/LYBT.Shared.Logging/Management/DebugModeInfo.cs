namespace LYBT.Shared.Logging.Management;

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
