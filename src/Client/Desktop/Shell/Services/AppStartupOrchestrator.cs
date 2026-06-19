using System.Windows;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Desktop.Shell.Views;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 封装启动流程：splash → pipeline → 显示主窗口。
/// 从 App.xaml.cs 抽取以降低复杂度。
/// </summary>
public class AppStartupOrchestrator
{
    private readonly IContainerProvider _container;
    private readonly ILogger<AppStartupOrchestrator> _logger;
    private SplashScreenWindow? _splash;

    public AppStartupOrchestrator(IContainerProvider container, ILogger<AppStartupOrchestrator> logger)
    {
        _container = container;
        _logger = logger;
    }

    public void ShowSplash()
    {
        _splash = new SplashScreenWindow();
        _splash.Show();
        _splash.UpdateStatus("正在初始化应用程序...");
    }

    public async Task RunStartupAsync(Window mainWindow)
    {
        try
        {
            _logger.LogInformation("启动管道开始执行");

            var pipeline = _container.Resolve<IStartupPipeline>();
            RegisterSteps(pipeline);

            var progress = new Progress<string>(msg => _splash?.UpdateStatus(msg));
            var result = await pipeline.ExecuteAsync(progress);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"启动步骤 '{result.FailedStepName}' 执行失败: {result.ErrorMessage}");
            }

            _logger.LogInformation("启动管道执行完成");
            await CloseSplashAsync();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "应用启动失败");
            _splash?.Close();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var message = ClientErrorMessageMapper.GetSafeOperationFailureMessage("启动", ex);
                MessageBox.Show(message, "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            });
        }
    }

    private void RegisterSteps(IStartupPipeline pipeline)
    {
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("ErrorHandling"));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("ModuleCoordinator"));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("CoreServices"));
        pipeline.RegisterStep(new LocalWebApiStartupStep(
            _container.Resolve<IEmbeddedLocalWebApiService>(),
            _container.Resolve<ILogger<LocalWebApiStartupStep>>()));
        pipeline.RegisterStep(new ApiHealthCheckStartupStep(
            _container.Resolve<IApplicationStateService>(),
            _container.Resolve<ILogger<ApiHealthCheckStartupStep>>(),
            timeoutSeconds: 5));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("Warmup"));
    }

    private async Task CloseSplashAsync()
    {
        if (_splash == null) return;
        _splash.FadeOut();
        await Task.Delay(400);
        _splash.Close();
        _splash = null;
    }
}
