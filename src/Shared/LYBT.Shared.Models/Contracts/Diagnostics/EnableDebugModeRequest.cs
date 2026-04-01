namespace LYBT.Shared.Models.Contracts.Diagnostics;

/// <summary>
/// 启用调试模式请求
/// </summary>
public class EnableDebugModeRequest
{
    /// <summary>
    /// 目标日志级别（Verbose/Debug/Information，默认Debug）
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 持续时间（分钟，默认30，最大120）
    /// </summary>
    public int? DurationMinutes { get; set; }
}
