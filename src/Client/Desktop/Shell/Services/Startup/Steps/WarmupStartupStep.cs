using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 应用预热启动步骤
/// 预加载常用资源和服务，优化首次使用体验
/// </summary>
public class WarmupStartupStep : IStartupStep
{
    private readonly ILogger<WarmupStartupStep> _logger;

    public WarmupStartupStep(ILogger<WarmupStartupStep> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "应用预热";

    /// <inheritdoc />
    public int Order => 50;

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <inheritdoc />
    public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在预热应用程序...");
        _logger.LogInformation("应用预热完成（预热逻辑已移至各服务内部按需执行）");
        return Task.FromResult(StartupStepResult.Succeeded(TimeSpan.Zero));
    }
}
