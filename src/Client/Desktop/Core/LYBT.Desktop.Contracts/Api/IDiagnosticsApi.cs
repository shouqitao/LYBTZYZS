using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Diagnostics;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// 系统诊断API客户端接口 - 对应服务端 DiagnosticsController
/// </summary>
/// <remarks>
/// 功能范围: 运行时日志级别查询、调试模式开关、日志级别手动设置
/// 权限要求: AdminOnly 策略（仅超级管理员/系统运维可访问）
/// 注: 使用 LYBT.Shared.Models.Contracts.Common.ApiResponse&lt;T&gt; 作为返回类型，
/// 与 IRegistrationApi 等其他 Refit 接口保持一致（不使用 Refit 原生 ApiResponse 包装）。
/// </remarks>
public interface IDiagnosticsApi
{
    /// <summary>
    /// 获取当前日志级别状态
    /// </summary>
    [Refit.Get("/api/v1/diagnostics/logging/status")]
    Task<ApiResponse<object>> GetLoggingStatusAsync();

    /// <summary>
    /// 启用调试模式 - 临时降低日志级别并设置自动过期
    /// </summary>
    /// <param name="request">调试模式请求（级别、持续时间）</param>
    [Refit.Post("/api/v1/diagnostics/logging/debug/enable")]
    Task<ApiResponse<object>> EnableDebugModeAsync([Refit.Body] EnableDebugModeRequest request);

    /// <summary>
    /// 禁用调试模式 - 恢复默认日志级别
    /// </summary>
    [Refit.Post("/api/v1/diagnostics/logging/debug/disable")]
    Task<ApiResponse<object>> DisableDebugModeAsync();

    /// <summary>
    /// 手动设置日志级别（无自动过期）
    /// </summary>
    /// <param name="request">目标级别请求</param>
    [Refit.Post("/api/v1/diagnostics/logging/level")]
    Task<ApiResponse<object>> SetLoggingLevelAsync([Refit.Body] SetLoggingLevelRequest request);
}
