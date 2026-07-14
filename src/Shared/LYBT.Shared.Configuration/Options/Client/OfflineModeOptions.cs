using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 离线模式配置
/// </summary>
public sealed class OfflineModeOptions
{
    public const string SectionName = "OfflineMode";

    /// <summary>
    /// 是否启用离线模式
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 本地 API 地址
    /// </summary>
    [Required(ErrorMessage = "OfflineMode:LocalApiBaseUrl 不能为空")]
    public string LocalApiBaseUrl { get; set; } = "http://localhost:5300";

    /// <summary>
    /// 远程不可用时是否自动切换到本地
    /// </summary>
    public bool AutoSwitchToLocal { get; set; } = true;

    /// <summary>
    /// 健康检查间隔 (秒)
    /// </summary>
    [Range(5, 300)]
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 切换前重试次数
    /// </summary>
    [Range(1, 10)]
    public int RetryCountBeforeSwitch { get; set; } = 3;
}
