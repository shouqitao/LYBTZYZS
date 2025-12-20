using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.Presentation.Notifications;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 错误处理初始化步骤
/// 注册全局异常处理器
/// </summary>
public class ErrorHandlingStartupStep : IStartupStep
{
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly ILogger<ErrorHandlingStartupStep> _logger;

    public ErrorHandlingStartupStep(
        IErrorHandlingService errorHandlingService,
        ILogger<ErrorHandlingStartupStep> logger)
    {
        _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "错误处理初始化";

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public bool IsRequired => true;

    /// <inheritdoc />
    public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在注册全局异常处理器...");

        try
        {
            _errorHandlingService.RegisterGlobalExceptionHandlers();
            _logger.LogInformation("全局异常处理器注册完成");

            return Task.FromResult(StartupStepResult.Succeeded(TimeSpan.Zero));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册全局异常处理器失败");
            return Task.FromResult(StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("注册全局异常处理器", ex), ex));
        }
    }
}
