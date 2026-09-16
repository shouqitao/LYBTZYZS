using System.Diagnostics;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 在主窗口显示后在后台运行启动管道。
/// D-2: 构造注入 IStartupPipeline + IEnumerable&lt;IStartupStep&gt;，不再依赖 IContainerProvider。
/// D-3: 启动失败通过 IUserNotificationService 通知用户，Service 层不直接调用 MessageBox。
/// </summary>
public class AppStartupOrchestrator
{
    private readonly IStartupPipeline _pipeline;
    private readonly IEnumerable<IStartupStep> _steps;
    private readonly ILogger<AppStartupOrchestrator> _logger;
    private readonly IUserNotificationService _notificationService;

    public AppStartupOrchestrator(
        IStartupPipeline pipeline,
        IEnumerable<IStartupStep> steps,
        ILogger<AppStartupOrchestrator> logger,
        IUserNotificationService notificationService)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task RunStartupAsync()
    {
        var totalStopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("启动管道开始执行");

            foreach (var step in _steps)
            {
                _pipeline.RegisterStep(step);
            }

            var result = await _pipeline.ExecuteAsync();

            if (!result.Success)
            {
                _logger.LogError("启动步骤 {Step} 失败: {Error}", result.FailedStepName, result.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("启动管道执行完成");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "应用启动失败");
            // D-3: Service 层不直接 MessageBox——经 IUserNotificationService 展示给用户
            await _notificationService.HandleExceptionAsync(ex, "启动");
        }
        finally
        {
            totalStopwatch.Stop();
            _logger.LogInformation("启动管道总耗时: {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);
        }
    }
}
