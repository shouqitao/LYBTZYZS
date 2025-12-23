using System.Collections.Concurrent;
using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// 可靠登出服务实现
/// OpenSpec: refactor-login-authentication (Phase 2.3, 3.2)
/// OpenSpec: unify-event-system (Phase 2.3)
/// 提供本地登出（立即生效）和服务端登出（可重试）的分离实现
/// 通过Prism EventAggregator发布登出事件
/// </summary>
public class LogoutService : ILogoutService, IDisposable
{
    private readonly ILogger<LogoutService> _logger;
    private readonly ITokenStorageService _tokenStorage;
    private readonly IAuthApi _authApi;
    private readonly ILoginStateMachine _loginStateMachine;
    private readonly IEventAggregator? _eventAggregator;
    private readonly ConcurrentQueue<PendingServerLogout> _pendingLogouts = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    /// <summary>
    /// 最大重试次数
    /// </summary>
    private const int MaxRetryCount = 3;

    /// <summary>
    /// 重试间隔（毫秒）
    /// </summary>
    private static readonly int[] RetryDelays = { 1000, 5000, 15000 };

    public LogoutService(
        ILogger<LogoutService> logger,
        ITokenStorageService tokenStorage,
        IAuthApi authApi,
        ILoginStateMachine loginStateMachine,
        IEventAggregator? eventAggregator = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
        _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
        _loginStateMachine = loginStateMachine ?? throw new ArgumentNullException(nameof(loginStateMachine));
        _eventAggregator = eventAggregator;
    }

    /// <inheritdoc />
    public int PendingServerLogoutCount => _pendingLogouts.Count;

    /// <inheritdoc />
    public async Task<LogoutResult> LogoutAsync()
    {
        _logger.LogInformation("开始可靠登出流程");

        // 触发状态机开始登出
        _loginStateMachine.Fire(LoginTrigger.StartLogout);

        // 获取登出所需信息（在清除前获取）
        var loginResponse = await _tokenStorage.GetLoginResponseAsync();
        var username = loginResponse?.User?.UserName;
        var refreshToken = loginResponse?.RefreshToken;

        // Step 1: 本地登出（始终成功）
        await ExecuteLocalLogoutAsync();

        // 触发状态机登出成功（本地已完成）
        _loginStateMachine.Fire(LoginTrigger.LogoutSuccess);

        // Step 2: 尝试服务端登出
        var serverResult = await TryServerLogoutAsync(username, refreshToken);

        // 发布登出完成事件（Phase 3.2）
        PublishLogoutCompletedEvent(username, serverResult);

        if (serverResult.Success)
        {
            _logger.LogInformation("登出流程完成 - 本地和服务端都已成功");
            return LogoutResult.FullSuccess("登出成功");
        }

        if (serverResult.QueuedForRetry)
        {
            _logger.LogWarning("登出流程完成 - 本地成功，服务端已加入重试队列");
            return LogoutResult.LocalSuccessServerQueued("本地登出成功，服务端登出将在网络恢复后重试");
        }

        _logger.LogWarning("登出流程完成 - 仅本地成功，服务端登出失败");
        return LogoutResult.LocalSuccessOnly("本地登出成功");
    }

    /// <summary>
    /// 发布登出完成事件
    /// </summary>
    private void PublishLogoutCompletedEvent(string? username, ServerLogoutAttemptResult serverResult)
    {
        if (_eventAggregator == null)
            return;

        try
        {
            var payload = new LogoutCompletedPayload
            {
                UserName = username,
                LocalLogoutCompleted = true,
                ServerLogoutCompleted = serverResult.Success,
                ServerLogoutQueued = serverResult.QueuedForRetry
            };
            _eventAggregator.GetEvent<AuthEvents.LogoutCompletedEvent>().Publish(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布登出完成事件失败");
        }
    }

    /// <inheritdoc />
    public async Task ExecuteLocalLogoutAsync()
    {
        _logger.LogDebug("执行本地登出");

        try
        {
            await _tokenStorage.ClearAuthenticationAsync();
            _logger.LogDebug("本地认证信息已清除");
        }
        catch (Exception ex)
        {
            // 本地登出不应该失败，但记录异常
            _logger.LogWarning(ex, "清除本地认证信息时发生异常，继续登出流程");
        }
    }

    /// <inheritdoc />
    public async Task<int> ProcessPendingServerLogoutsAsync()
    {
        if (_pendingLogouts.IsEmpty)
        {
            return 0;
        }

        // 使用锁防止并发处理
        if (!await _processingLock.WaitAsync(0))
        {
            _logger.LogDebug("已有其他任务在处理待重试队列");
            return 0;
        }

        try
        {
            var processedCount = 0;
            var remainingItems = new List<PendingServerLogout>();

            while (_pendingLogouts.TryDequeue(out var pending))
            {
                var result = await ExecuteServerLogoutWithRetryAsync(
                    pending.UserName,
                    pending.RefreshToken,
                    pending.RetryCount);

                if (result.Success)
                {
                    processedCount++;
                    _logger.LogInformation("待重试的服务端登出已完成 [用户: {UserName}]", pending.UserName);
                }
                else if (result.ShouldRetry)
                {
                    // 放回队列
                    remainingItems.Add(new PendingServerLogout
                    {
                        UserName = pending.UserName,
                        RefreshToken = pending.RefreshToken,
                        RetryCount = pending.RetryCount + 1,
                        QueuedAt = pending.QueuedAt
                    });
                }
                else
                {
                    // 达到最大重试次数或不可恢复的错误
                    _logger.LogWarning("服务端登出最终失败 [用户: {UserName}, 重试次数: {RetryCount}]",
                        pending.UserName, pending.RetryCount);
                }
            }

            // 将需要继续重试的项放回队列
            foreach (var item in remainingItems)
            {
                _pendingLogouts.Enqueue(item);
            }

            if (_pendingLogouts.IsEmpty)
            {
                PublishPendingLogoutsClearedEvent(processedCount);
            }

            return processedCount;
        }
        finally
        {
            _processingLock.Release();
        }
    }

    /// <summary>
    /// 尝试执行服务端登出
    /// </summary>
    private async Task<ServerLogoutAttemptResult> TryServerLogoutAsync(string? username, string? refreshToken)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogDebug("无用户信息，跳过服务端登出");
            return ServerLogoutAttemptResult.CreateSuccess();
        }

        var result = await ExecuteServerLogoutWithRetryAsync(username, refreshToken, retryCount: 0);

        if (!result.Success && result.ShouldRetry)
        {
            // 加入重试队列
            var pending = new PendingServerLogout
            {
                UserName = username,
                RefreshToken = refreshToken,
                RetryCount = 1,
                QueuedAt = DateTime.UtcNow
            };
            _pendingLogouts.Enqueue(pending);

            // 发布Prism PubSubEvent
            PublishServerLogoutFailedEvent(username, result.FailureReason, result.ErrorMessage, true, 0);

            return new ServerLogoutAttemptResult
            {
                Success = false,
                QueuedForRetry = true,
                FailureReason = result.FailureReason
            };
        }

        return result;
    }

    /// <summary>
    /// 发布待处理登出已清空事件
    /// </summary>
    private void PublishPendingLogoutsClearedEvent(int processedCount)
    {
        if (_eventAggregator == null)
            return;

        try
        {
            var payload = new PendingLogoutsClearedPayload
            {
                ProcessedCount = processedCount
            };
            _eventAggregator.GetEvent<AuthEvents.PendingLogoutsClearedEvent>().Publish(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布待处理登出已清空事件失败");
        }
    }

    /// <summary>
    /// 发布服务端登出失败事件
    /// </summary>
    private void PublishServerLogoutFailedEvent(
        string? username,
        ServerLogoutFailureReason reason,
        string? errorMessage,
        bool queuedForRetry,
        int retryCount)
    {
        if (_eventAggregator == null)
            return;

        try
        {
            var payload = new ServerLogoutFailedPayload
            {
                UserName = username,
                Reason = reason,
                ErrorMessage = errorMessage,
                QueuedForRetry = queuedForRetry,
                RetryCount = retryCount
            };
            _eventAggregator.GetEvent<AuthEvents.ServerLogoutFailedEvent>().Publish(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布服务端登出失败事件失败");
        }
    }

    /// <summary>
    /// 带重试的服务端登出执行
    /// </summary>
    private async Task<ServerLogoutAttemptResult> ExecuteServerLogoutWithRetryAsync(
        string? username,
        string? refreshToken,
        int retryCount)
    {
        var maxAttempts = retryCount == 0 ? 1 : 1; // 单次尝试，重试由队列处理

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                _logger.LogDebug("尝试服务端登出 [用户: {UserName}, 尝试: {Attempt}]", username, attempt + 1);

                var logoutRequest = new LogoutRequest
                {
                    UserName = username ?? string.Empty,
                    RefreshToken = refreshToken
                };

                var response = await _authApi.LogoutAsync(logoutRequest);

                if (response.Success)
                {
                    _logger.LogDebug("服务端登出成功");
                    return ServerLogoutAttemptResult.CreateSuccess();
                }

                // 服务端返回失败，但可能Token已失效
                if (response.Message?.Contains("401") == true ||
                    response.Message?.Contains("Unauthorized") == true ||
                    response.Message?.Contains("Token") == true)
                {
                    _logger.LogDebug("Token已失效，无需重试服务端登出");
                    return ServerLogoutAttemptResult.CreateSuccess(); // 视为成功
                }

                _logger.LogWarning("服务端登出返回失败: {Message}", response.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "服务端登出网络异常");
                return new ServerLogoutAttemptResult
                {
                    Success = false,
                    ShouldRetry = retryCount < MaxRetryCount,
                    FailureReason = ServerLogoutFailureReason.NetworkUnavailable,
                    ErrorMessage = ex.Message
                };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "服务端登出超时");
                return new ServerLogoutAttemptResult
                {
                    Success = false,
                    ShouldRetry = retryCount < MaxRetryCount,
                    FailureReason = ServerLogoutFailureReason.Timeout,
                    ErrorMessage = "请求超时"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "服务端登出异常");
                return new ServerLogoutAttemptResult
                {
                    Success = false,
                    ShouldRetry = retryCount < MaxRetryCount,
                    FailureReason = ServerLogoutFailureReason.ServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        return new ServerLogoutAttemptResult
        {
            Success = false,
            ShouldRetry = retryCount < MaxRetryCount,
            FailureReason = ServerLogoutFailureReason.ServerError,
            ErrorMessage = "服务端登出失败"
        };
    }

    /// <summary>
    /// 待重试的服务端登出信息
    /// </summary>
    private class PendingServerLogout
    {
        public string? UserName { get; init; }
        public string? RefreshToken { get; init; }
        public int RetryCount { get; init; }
        public DateTime QueuedAt { get; init; }
    }

    /// <summary>
    /// 服务端登出尝试结果
    /// </summary>
    private class ServerLogoutAttemptResult
    {
        public bool Success { get; init; }
        public bool ShouldRetry { get; init; }
        public bool QueuedForRetry { get; init; }
        public ServerLogoutFailureReason FailureReason { get; init; }
        public string? ErrorMessage { get; init; }

        public static ServerLogoutAttemptResult CreateSuccess() =>
            new() { Success = true, ShouldRetry = false };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _processingLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
