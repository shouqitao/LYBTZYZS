using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LYBT.Core.Infrastructure.Security
{
    /// <summary>
    /// 密钥自动旋转后台服务
    /// </summary>
    public class KeyRotationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KeyRotationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // 每6小时检查一次

        public KeyRotationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<KeyRotationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("密钥旋转后台服务已启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndRotateKeysAsync(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // 服务停止时的正常取消
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "密钥旋转检查过程中发生错误");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // 出错后等待5分钟再试
                }
            }

            _logger.LogInformation("密钥旋转后台服务已停止");
        }

        private async Task CheckAndRotateKeysAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var keyManagementService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();

            // 检查JWT密钥是否需要旋转（默认90天旋转一次）
            if (await keyManagementService.IsRotationRequiredAsync("JWT_SECRET"))
            {
                _logger.LogInformation("开始旋转JWT密钥");
                try
                {
                    await keyManagementService.RotateJwtSecretAsync();
                    _logger.LogInformation("JWT密钥旋转成功");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JWT密钥旋转失败");
                }
            }

            // 检查API密钥
            var apiKeyTypes = new[] { "API_KEY_EXTERNAL", "API_KEY_INTERNAL", "API_KEY_MOBILE" };
            foreach (var keyType in apiKeyTypes)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (await keyManagementService.IsRotationRequiredAsync(keyType))
                {
                    _logger.LogInformation("API密钥 {KeyType} 需要旋转", keyType);
                    // 这里可以发送通知或记录到审计日志
                    // API密钥通常需要手动旋转，因为需要通知使用方
                }
            }

            _logger.LogDebug("密钥旋转检查完成");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在停止密钥旋转后台服务");
            return base.StopAsync(cancellationToken);
        }
    }
}