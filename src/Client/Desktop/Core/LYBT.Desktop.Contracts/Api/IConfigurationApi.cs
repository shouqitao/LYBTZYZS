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
internal interface IConfigurationApi
{
    /// <summary>
    /// 获取安全配置项集合
    /// </summary>
    [Refit.Get("/api/v1/configuration")]
    Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取单个配置项
    /// </summary>
    /// <param name="key">配置项名称（如 App:Name）</param>
    /// <param name="ct">取消令牌</param>
    [Refit.Get("/api/v1/configuration/{key}")]
    Task<ApiResponse<string>> GetValueAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 修改单个配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    /// <param name="key">配置项名称（如 App:Name）</param>
    /// <param name="value">新值</param>
    /// <param name="ct">取消令牌</param>
    [Refit.Put("/api/v1/configuration/{key}")]
    Task<ApiResponse> SetValueAsync(string key, [Refit.Body] string value, CancellationToken ct = default);

    /// <summary>
    /// 批量修改配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    /// <param name="settings">配置项键值集合</param>
    /// <param name="ct">取消令牌</param>
    [Refit.Put("/api/v1/configuration")]
    Task<ApiResponse> UpdateConfigurationAsync([Refit.Body] Dictionary<string, string> settings, CancellationToken ct = default);

    /// <summary>
    /// 验证生产环境配置
    /// </summary>
    [Refit.Post("/api/v1/configuration/validate")]
    Task<ApiResponse> ValidateProductionAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取单节配置（SHELL-018 Phase 1: 敏感键掩码）
    /// </summary>
    [Refit.Get("/api/v1/configuration/sections/{section}")]
    Task<ApiResponse<Dictionary<string, string>>> GetSectionAsync(string section, CancellationToken ct = default);

    /// <summary>
    /// 修改单节配置（SHELL-018 Phase 1: 白名单逐键 + 生效语义）
    /// </summary>
    [Refit.Put("/api/v1/configuration/sections/{section}")]
    Task<ApiResponse<ConfigUpdateResultDto>> UpdateSectionAsync(string section, [Refit.Body] Dictionary<string, string> values, CancellationToken ct = default);

    /// <summary>
    /// 延迟重启服务（SHELL-018 Phase 1: 30 秒后 StopApplication）
    /// </summary>
    [Refit.Post("/api/v1/configuration/restart")]
    Task<ApiResponse> RestartAsync(CancellationToken ct = default);
}
