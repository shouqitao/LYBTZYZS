namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 模式切换验证器 - 切换前置检查 (US-SYNC-008)
/// </summary>
public interface IModeSwitchValidator
{
    /// <summary>
    /// 验证本地 -> 远程切换的前置条件 (SYNC-D01)
    /// 检查本地是否有 Active/Suspended 医案
    /// </summary>
    Task<ModeSwitchValidationResult> ValidateLocalToRemoteSwitchAsync(CancellationToken ct = default);

    /// <summary>
    /// 验证远程 -> 本地切换的前置条件
    /// 检查 LocalDB 是否可用
    /// </summary>
    Task<ModeSwitchValidationResult> ValidateRemoteToLocalSwitchAsync(CancellationToken ct = default);
}

/// <summary>
/// 模式切换验证结果
/// </summary>
public record ModeSwitchValidationResult(
    bool IsValid,
    string? ErrorMessage = null,
    int? UnfinishedCaseCount = null)
{
    public static readonly ModeSwitchValidationResult Valid = new(true);

    public static ModeSwitchValidationResult Failed(string errorMessage, int? unfinishedCaseCount = null)
        => new(false, errorMessage, unfinishedCaseCount);
}
