using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 客户端会话配置
/// </summary>
public sealed class ClientSessionOptions
{
    public const string SectionName = ConfigurationSections.ClientSession;

    /// <summary>
    /// 无活动超时时间 (分钟)
    /// </summary>
    [Range(1, 120)]
    public int InactivityTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// 超时前警告时间 (分钟)
    /// </summary>
    [Range(0, 10)]
    public int WarningBeforeTimeoutMinutes { get; set; } = 0;

    /// <summary>
    /// 活动检查间隔 (秒)
    /// </summary>
    [Range(10, 120)]
    public int ActivityCheckIntervalSeconds { get; set; } = 30;
}
