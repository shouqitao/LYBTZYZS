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
    /// 获取单节配置（SHELL-018 Phase 1: 敏感键掩码脱敏）
    /// </summary>
    Task<Result<Dictionary<string, string>>> GetSectionAsync(string section, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量修改单节配置（SHELL-018 Phase 1: 白名单逐键 + 持久化 + Reload）
    /// </summary>
    Task<Result<ConfigUpdateResultDto>> UpdateSectionAsync(string section, Dictionary<string, string> values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改单个配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    Task<Result> SetValueAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量修改配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    Task<Result> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// 调度延迟重启（P1-1 2026-08-14: 限频滑动窗口 + 30s 延迟移入 Service——原 Controller 内 TryAcquireRestartSlot+Task.Run）
    /// </summary>
    /// <param name="lifetime">宿主生命周期（停止应用）</param>
    /// <returns>成功 = 已调度；失败 = 限频拒绝</returns>
    Task<Result> ScheduleRestartAsync(Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime, CancellationToken cancellationToken = default);
}


