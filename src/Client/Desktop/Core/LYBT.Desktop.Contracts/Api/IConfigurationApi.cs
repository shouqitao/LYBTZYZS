using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// 系统配置 API 客户端接口 - 对应服务端 ConfigurationController
/// </summary>
/// <remarks>
/// 功能范围: 安全配置项读取（App:Name/App:Version/App:Environment）、单值/批量修改（白名单校验）、生产配置验证
/// 权限要求: AdminOrSuperAdmin 策略（管理员/系统运维可访问）
/// 注: 使用 LYBT.Shared.Models.Contracts.Common.ApiResponse&lt;T&gt; 作为返回类型，
/// 与 IDiagnosticsApi 等其他 Refit 接口保持一致（不使用 Refit 原生 ApiResponse 包装）。
/// </remarks>
public interface IConfigurationApi
{
    /// <summary>
    /// 获取安全配置项集合
    /// </summary>
    [Refit.Get("/api/v1/configuration")]
    Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync();

    /// <summary>
    /// 获取单个配置项
    /// </summary>
    /// <param name="key">配置项名称（如 App:Name）</param>
    [Refit.Get("/api/v1/configuration/{key}")]
    Task<ApiResponse<string>> GetValueAsync(string key);

    /// <summary>
    /// 修改单个配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    /// <param name="key">配置项名称（如 App:Name）</param>
    /// <param name="value">新值</param>
    [Refit.Put("/api/v1/configuration/{key}")]
    Task<ApiResponse> SetValueAsync(string key, [Refit.Body] string value);

    /// <summary>
    /// 批量修改配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    /// <param name="settings">配置项键值集合</param>
    [Refit.Put("/api/v1/configuration")]
    Task<ApiResponse> UpdateConfigurationAsync([Refit.Body] Dictionary<string, string> settings);

    /// <summary>
    /// 验证生产环境配置
    /// </summary>
    [Refit.Post("/api/v1/configuration/validate")]
    Task<ApiResponse> ValidateProductionAsync();
}
