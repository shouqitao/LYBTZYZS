using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Security;

/// <summary>
/// 密钥自动旋转后台服务
/// 使用工厂模式避免Service Locator反模式
/// </summary>
public class KeyRotationBackgroundService : BackgroundService
{
    private readonly IKeyManagementServiceFactory _keyManagementServiceFactory;
    private readonly ILogger<KeyRotationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // 每小时检查一次

    public KeyRotationBackgroundService(
        IKeyManagementServiceFactory keyManagementServiceFactory,
        ILogger<KeyRotationBackgroundService> logger)
    {
        _keyManagementServiceFactory = keyManagementServiceFactory ?? throw new ArgumentNullException(nameof(keyManagementServiceFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("密钥旋转后台服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRotateKeysAsync();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 服务停止时的正常取消
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密钥轮换检查时发生错误");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // 出错后等待5分钟再试
            }
        }

        _logger.LogInformation("密钥旋转后台服务已停止");
    }

    private async Task CheckAndRotateKeysAsync()
    {
        try
        {
            // 使用工厂创建密钥管理服务实例
            var keyManagementService = _keyManagementServiceFactory.CreateKeyManagementService();

            if (keyManagementService == null)
            {
                _logger.LogError("无法创建密钥管理服务实例");
                return;
            }

            // 检查是否需要轮换密钥
            if (await keyManagementService.ShouldRotateKeyAsync())
            {
                _logger.LogInformation("开始JWT密钥轮换");

                var newSecret = await keyManagementService.RotateJwtSecretAsync();

                _logger.LogInformation("JWT密钥轮换成功完成");
            }
            else
            {
                _logger.LogDebug("当前无需进行密钥轮换");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查和轮换密钥过程中发生错误");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止密钥旋转后台服务");
        return base.StopAsync(cancellationToken);
    }
}
