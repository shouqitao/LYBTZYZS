using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 服务端会话配置
/// </summary>
public sealed class SessionOptions
{
    public const string SectionName = ConfigurationSections.Session;

    /// <summary>
    /// 会话超时时间 (分钟)
    /// </summary>
    [Range(5, 1440)]
    public int TimeoutMinutes { get; set; } = 120;

    /// <summary>
    /// 是否允许并发会话
    /// </summary>
    public bool AllowConcurrentSessions { get; set; } = false;

    /// <summary>
    /// 滑动过期
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;
}
