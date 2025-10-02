using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Security;

/// <summary>
/// 密钥管理服务工厂实现
/// </summary>
public class KeyManagementServiceFactory : IKeyManagementServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public KeyManagementServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 创建密钥管理服务实例
    /// </summary>
    public IKeyManagementService CreateKeyManagementService()
    {
        var logger = _serviceProvider.GetService<ILogger<KeyManagementService>>();
        var jwtOptions = _serviceProvider.GetService<IOptions<JwtOptions>>();

        if (logger == null)
        {
            throw new InvalidOperationException("无法从服务容器中获取 ILogger<KeyManagementService> 服务");
        }

        if (jwtOptions == null)
        {
            throw new InvalidOperationException("无法从服务容器中获取 IOptions<JwtOptions> 服务");
        }

        return new KeyManagementService(logger, jwtOptions);
    }
}
