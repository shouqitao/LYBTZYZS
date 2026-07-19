using System.Diagnostics;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// Runs the startup pipeline in the background after the main window is shown.
/// </summary>
public class AppStartupOrchestrator
{
    private readonly IContainerProvider _container;
    private readonly ILogger<AppStartupOrchestrator> _logger;

    public AppStartupOrchestrator(IContainerProvider container, ILogger<AppStartupOrchestrator> logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task RunStartupAsync()
    {
        var totalStopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("启动管道开始执行");

            var pipeline = _container.Resolve<IStartupPipeline>();
            RegisterSteps(pipeline);

            var result = await pipeline.ExecuteAsync();

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
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var message = ClientErrorMessageMapper.GetSafeOperationFailureMessage("启动", ex);
                System.Windows.MessageBox.Show(message, "启动失败",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            });
        }
        finally
        {
            totalStopwatch.Stop();
            _logger.LogInformation("启动管道总耗时: {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);
        }
    }

    private void RegisterSteps(IStartupPipeline pipeline)
    {
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("ErrorHandling"));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("ModuleCoordinator"));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("LocalWebApi"));
        pipeline.RegisterStep(_container.Resolve<ApiHealthCheckStartupStep>());
    }
}
