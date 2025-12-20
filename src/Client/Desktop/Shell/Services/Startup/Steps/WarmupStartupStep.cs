using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Localization;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 应用预热启动步骤
/// 预加载常用资源和服务，优化首次使用体验
/// </summary>
public class WarmupStartupStep : IStartupStep
{
    private readonly IStartupOptimizationService _startupOptimizationService;
    private readonly ILogger<WarmupStartupStep> _logger;

    public WarmupStartupStep(
        IStartupOptimizationService startupOptimizationService,
        ILogger<WarmupStartupStep> logger)
    {
        _startupOptimizationService = startupOptimizationService ?? throw new ArgumentNullException(nameof(startupOptimizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "应用预热";

    /// <inheritdoc />
    public int Order => 50;

    /// <inheritdoc />
    public bool IsRequired => false; // 预热失败不应阻塞启动

    /// <inheritdoc />
    public async Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在预热应用程序...");

        try
        {
            await _startupOptimizationService.WarmupApplicationAsync();
            _logger.LogInformation("应用预热完成");

            return StartupStepResult.Succeeded(TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "应用预热失败，但不影响主流程");
            return StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("应用预热", ex), ex);
        }
    }
}
