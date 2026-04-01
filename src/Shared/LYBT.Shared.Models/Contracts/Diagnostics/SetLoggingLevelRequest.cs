namespace LYBT.Shared.Models.Contracts.Diagnostics;

/// <summary>
/// 设置日志级别请求
/// </summary>
public class SetLoggingLevelRequest
{
    /// <summary>
    /// 目标日志级别（Verbose/Debug/Information/Warning/Error/Fatal）
    /// </summary>
    public string Level { get; set; } = string.Empty;
}
