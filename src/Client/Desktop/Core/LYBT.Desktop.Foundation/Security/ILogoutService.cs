using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// 可靠登出服务接口
/// OpenSpec: refactor-login-authentication (Phase 2.3)
/// OpenSpec: unify-event-system (Phase 2.3)
/// 提供本地登出（立即生效）和服务端登出（可重试）的分离实现
/// </summary>
/// <remarks>
/// 登出事件通过Prism PubSubEvent发布:
/// - AuthEvents.LogoutCompletedEvent: 登出完成
/// - AuthEvents.ServerLogoutFailedEvent: 服务端登出失败
/// - AuthEvents.PendingLogoutsClearedEvent: 待处理登出已清空
/// </remarks>
public interface ILogoutService
{
    /// <summary>
    /// 执行完整登出流程
    /// 本地登出立即生效，服务端登出异步执行（失败时自动加入重试队列）
    /// </summary>
    /// <returns>登出结果</returns>
    Task<LogoutResult> LogoutAsync();

    /// <summary>
    /// 仅执行本地登出
    /// 清除本地Token和会话状态，不调用服务端API
    /// 此操作始终成功
    /// </summary>
    /// <returns>任务</returns>
    Task ExecuteLocalLogoutAsync();

    /// <summary>
    /// 尝试处理待重试的服务端登出请求
    /// 当网络恢复时应调用此方法
    /// </summary>
    /// <returns>成功处理的数量</returns>
    Task<int> ProcessPendingServerLogoutsAsync();

    /// <summary>
    /// 获取待处理的服务端登出请求数量
    /// </summary>
    int PendingServerLogoutCount { get; }
}

/// <summary>
/// 登出结果
/// </summary>
/// <param name="Success">是否成功（本地登出成功即视为成功）</param>
/// <param name="LocalLogoutCompleted">本地登出是否完成</param>
/// <param name="ServerLogoutCompleted">服务端登出是否完成</param>
/// <param name="ServerLogoutQueued">服务端登出是否已加入重试队列</param>
/// <param name="Message">结果消息</param>
public record LogoutResult(
    bool Success,
    bool LocalLogoutCompleted,
    bool ServerLogoutCompleted,
    bool ServerLogoutQueued,
    string? Message = null)
{
    /// <summary>
    /// 创建完全成功的结果
    /// </summary>
    public static LogoutResult FullSuccess(string? message = null) =>
        new(Success: true, LocalLogoutCompleted: true, ServerLogoutCompleted: true, ServerLogoutQueued: false, message);

    /// <summary>
    /// 创建本地成功、服务端已加入队列的结果
    /// </summary>
    public static LogoutResult LocalSuccessServerQueued(string? message = null) =>
        new(Success: true, LocalLogoutCompleted: true, ServerLogoutCompleted: false, ServerLogoutQueued: true, message);

    /// <summary>
    /// 创建仅本地成功的结果
    /// </summary>
    public static LogoutResult LocalSuccessOnly(string? message = null) =>
        new(Success: true, LocalLogoutCompleted: true, ServerLogoutCompleted: false, ServerLogoutQueued: false, message);
}

/// <summary>
/// 服务端登出失败事件参数
/// </summary>
public class ServerLogoutFailedEventArgs : EventArgs
{
    /// <summary>
    /// 失败的用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public ServerLogoutFailureReason Reason { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 是否已加入重试队列
    /// </summary>
    public bool QueuedForRetry { get; init; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; init; }
}

/// <summary>
/// 服务端登出失败原因
/// </summary>
public enum ServerLogoutFailureReason
{
    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown,

    /// <summary>
    /// 网络不可用
    /// </summary>
    NetworkUnavailable,

    /// <summary>
    /// 服务器错误
    /// </summary>
    ServerError,

    /// <summary>
    /// 请求超时
    /// </summary>
    Timeout,

    /// <summary>
    /// Token已失效（无需重试）
    /// </summary>
    TokenInvalid,

    /// <summary>
    /// 达到最大重试次数
    /// </summary>
    MaxRetriesExceeded
}
