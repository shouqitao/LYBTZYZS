using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 错误处理初始化步骤
/// 注册全局异常处理器
/// optimize-desktop-core: 统一使用Shared.ExceptionHandling
/// </summary>
public class ErrorHandlingStartupStep : IStartupStep
{
    private readonly IDesktopExceptionHandler _exceptionHandler;
    private readonly ILogger<ErrorHandlingStartupStep> _logger;

    public ErrorHandlingStartupStep(
        IDesktopExceptionHandler exceptionHandler,
        ILogger<ErrorHandlingStartupStep> logger)
    {
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
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
            _exceptionHandler.RegisterGlobalExceptionHandlers();
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
