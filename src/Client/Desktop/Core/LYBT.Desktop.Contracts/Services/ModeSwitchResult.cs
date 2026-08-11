namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 模式切换结果（B2 US-SHELL-007）。
/// 失败时 <see cref="Succeeded"/> 为 false，<see cref="ErrorCode"/> 给出阻断原因，
/// 当前模式保持不变（自动回退到切换前模式）。
/// </summary>
public sealed record ModeSwitchResult(bool Succeeded, string? ErrorCode = null, string? Message = null)
{
    /// <summary>ERR-70506：本地有未完成医案（Active/Suspended），阻断切换到远程。</summary>
    public const string PendingCasesBlockedCode = "ERR-70506";

    public static ModeSwitchResult Success() => new(true);

    public static ModeSwitchResult Blocked(string errorCode, string message) => new(false, errorCode, message);

    public static ModeSwitchResult PendingCasesBlocked(int count) =>
        Blocked(PendingCasesBlockedCode, $"本地有 {count} 个未完成医案（进行中/挂起），请先处理或关闭后再切换到远程模式");
}
