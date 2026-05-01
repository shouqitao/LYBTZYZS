using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.Infrastructure.Configuration.Services;

/// <summary>
/// 系统配置服务接口
/// </summary>
public interface ISystemConfigurationService
{
    /// <summary>
    /// 获取系统配置项
    /// </summary>
    Task<Result<Dictionary<string, string>>> GetConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个配置项
    /// </summary>
    Task<Result<string?>> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证生产环境配置
    /// </summary>
    Task<Result> ValidateProductionConfigAsync(CancellationToken cancellationToken = default);
}
