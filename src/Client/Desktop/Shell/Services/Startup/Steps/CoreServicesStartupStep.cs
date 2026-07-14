using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 核心服务初始化步骤
/// 初始化应用程序核心基础服务
/// </summary>
public class CoreServicesStartupStep : IStartupStep
{
    private readonly IApplicationInitializationService _initializationService;
    private readonly ILogger<CoreServicesStartupStep> _logger;

    public CoreServicesStartupStep(
        IApplicationInitializationService initializationService,
        ILogger<CoreServicesStartupStep> logger)
    {
        _initializationService = initializationService ?? throw new ArgumentNullException(nameof(initializationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "核心服务初始化";

    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    /// <remarks>与 ModuleCoordinatorStartupStep 并行执行（同一 ParallelGroup）</remarks>
    public string? ParallelGroup => "CoreInit";

    /// <inheritdoc />
    public bool IsRequired => true;

    /// <inheritdoc />
    public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("核心服务初始化（已由其他步骤处理）...");

        try
        {
            // 核心服务初始化已由以下步骤分别处理：
            // - ErrorHandlingStartupStep: 错误处理 (Order: 10)
            // - ModuleCoordinatorStartupStep: 模块协调器 (Order: 20)
            // - WarmupStartupStep: 应用预热 (Order: 50)
            _logger.LogInformation("核心服务初始化完成（委托给专用步骤）");

            return Task.FromResult(StartupStepResult.Succeeded(TimeSpan.Zero));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "核心服务初始化失败");
            return Task.FromResult(StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("核心服务初始化", ex), ex));
        }
    }
}
