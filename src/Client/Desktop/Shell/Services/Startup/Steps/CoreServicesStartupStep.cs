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
    public bool IsRequired => true;

    /// <inheritdoc />
    public async Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在初始化核心服务...");

        try
        {
            await _initializationService.InitializeCoreServicesAsync();
            _logger.LogInformation("核心服务初始化完成");

            return StartupStepResult.Succeeded(TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "核心服务初始化失败");
            return StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("核心服务初始化", ex), ex);
        }
    }
}
