using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Localization;
using Microsoft.Extensions.Logging;
using Prism.Modularity;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 模块协调器初始化步骤
/// 订阅模块加载事件
/// </summary>
public class ModuleCoordinatorStartupStep : IStartupStep
{
    private readonly IModuleManager _moduleManager;
    private readonly ILogger<ModuleCoordinatorStartupStep> _logger;
    private readonly Dictionary<string, DateTime> _moduleInitTimes = new();

    public ModuleCoordinatorStartupStep(
        IModuleManager moduleManager,
        ILogger<ModuleCoordinatorStartupStep> logger)
    {
        _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "模块协调器初始化";

    /// <inheritdoc />
    public int Order => 20;

    /// <inheritdoc />
    public bool IsRequired => false; // 模块协调器失败不应阻塞启动

    /// <inheritdoc />
    public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在初始化模块协调器...");

        try
        {
            SubscribeToModuleEvents();
            _logger.LogInformation("模块协调器初始化完成");

            return Task.FromResult(StartupStepResult.Succeeded(TimeSpan.Zero));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "模块协调器初始化失败，但不影响主流程");
            return Task.FromResult(StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("模块协调器初始化", ex), ex));
        }
    }

    /// <summary>
    /// 订阅模块事件
    /// </summary>
    private void SubscribeToModuleEvents()
    {
        // 模块开始加载事件
        _moduleManager.ModuleDownloadProgressChanged += (sender, e) =>
        {
            if (e.ProgressPercentage == 0) // 开始加载
            {
                _moduleInitTimes[e.ModuleInfo.ModuleName] = DateTime.Now;
                _logger.LogDebug("模块 {ModuleName} 开始加载", e.ModuleInfo.ModuleName);
            }
        };

        // 模块加载完成事件
        _moduleManager.LoadModuleCompleted += (sender, e) =>
        {
            var moduleName = e.ModuleInfo.ModuleName;
            if (_moduleInitTimes.TryGetValue(moduleName, out var startTime))
            {
                var initializationTime = DateTime.Now - startTime;
                _moduleInitTimes.Remove(moduleName);

                _logger.LogInformation("模块 {ModuleName} 加载完成，耗时 {ElapsedTime}ms",
                    moduleName, initializationTime.TotalMilliseconds);
            }

            if (e.Error != null)
            {
                _logger.LogError(e.Error, "模块 {ModuleName} 加载失败", moduleName);
            }
        };
    }
}
