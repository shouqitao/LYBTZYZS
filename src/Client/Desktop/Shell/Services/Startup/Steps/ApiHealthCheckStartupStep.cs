using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// API健康检查启动步骤
/// 验证后端API服务可用性
/// </summary>
public class ApiHealthCheckStartupStep : IStartupStep
{
    private readonly IApplicationStateService _applicationStateService;
    private readonly ILogger<ApiHealthCheckStartupStep> _logger;
    private readonly int _timeoutSeconds;

    public ApiHealthCheckStartupStep(
        IApplicationStateService applicationStateService,
        ILogger<ApiHealthCheckStartupStep> logger,
        int timeoutSeconds = 10)
    {
        _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeoutSeconds = timeoutSeconds;
    }

    /// <inheritdoc />
    public string Name => "API健康检查";

    /// <inheritdoc />
    public int Order => 40;

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <inheritdoc />
    public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("API健康检查将在后台进行...");

        // 在后台异步执行健康检查，不阻塞启动流程
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("[Startup] 后台API健康检查开始...");
                var isHealthy = await _applicationStateService.CheckApiHealthAsync(_timeoutSeconds);

                if (isHealthy)
                {
                    _logger.LogInformation("[Startup] API健康检查通过");
                }
                else
                {
                    _logger.LogWarning("[Startup] API健康检查未通过，服务可能不可用");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Startup] 后台API健康检查失败，将在HealthCheckCoordinator中重试");
            }
        }, cancellationToken);

        // 立即返回成功，不等待健康检查完成
        return Task.FromResult(StartupStepResult.Succeeded(TimeSpan.Zero));
    }
}
