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
    public bool IsRequired => true;

    /// <inheritdoc />
    public async Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在检查API连接...");

        try
        {
            var isHealthy = await _applicationStateService.CheckApiHealthAsync(_timeoutSeconds);

            if (isHealthy)
            {
                _logger.LogInformation("API健康检查通过");
                return StartupStepResult.Succeeded(TimeSpan.Zero);
            }
            else
            {
                _logger.LogWarning("API健康检查未通过，服务可能不可用");
                return StartupStepResult.Failed("API服务不可用，请检查WebAPI是否已启动");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API健康检查失败");
            return StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("API健康检查", ex), ex);
        }
    }
}
