using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;

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

    /// <summary>
    /// 修改单个配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    Task<Result> SetValueAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量修改配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    Task<Result> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default);
}


