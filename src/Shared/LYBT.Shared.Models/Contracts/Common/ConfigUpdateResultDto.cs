namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 配置节更新结果（SHELL-018 Phase 1: PUT /configuration/{section} 响应）
/// </summary>
public class ConfigUpdateResultDto
{
    /// <summary>是否已应用（白名单通过 + 写入成功）</summary>
    public bool Applied { get; set; }

    /// <summary>是否需重启生效（功能开关类热更新为 false）</summary>
    public bool RestartRequired { get; set; }

    /// <summary>生效模式（hot / restart）</summary>
    public string EffectiveMode { get; set; } = "restart";

    /// <summary>成功写入的键数</summary>
    public int UpdatedCount { get; set; }
}
