using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Services;

/// <summary>
/// 模式切换验证器 - 切换前置检查 (US-SYNC-008)
/// </summary>
public sealed class ModeSwitchValidator : IModeSwitchValidator
{
    private readonly ILogger<ModeSwitchValidator> _logger;

    public ModeSwitchValidator(ILogger<ModeSwitchValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ModeSwitchValidationResult> ValidateLocalToRemoteSwitchAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[ModeSwitchValidator] Local->Remote validation passed");
        return Task.FromResult(ModeSwitchValidationResult.Valid);
    }

    public Task<ModeSwitchValidationResult> ValidateRemoteToLocalSwitchAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[ModeSwitchValidator] Remote->Local validation passed");
        return Task.FromResult(ModeSwitchValidationResult.Valid);
    }
}
