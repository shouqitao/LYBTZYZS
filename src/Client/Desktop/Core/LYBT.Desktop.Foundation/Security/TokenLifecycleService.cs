using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token生命周期服务实现
    /// Issue #1864: 客户端Token生命周期管理
    /// </summary>
    /// <remarks>
    /// 状态机转换：
    /// - NotAuthenticated -> Active (登录成功)
    /// - Active -> Warning (剩余时间少于阈值)
    /// - Active -> Expired (Token过期)
    /// - Warning -> Active (刷新成功)
    /// - Warning -> Expired (刷新失败或超时)
    /// - Expired -> NotAuthenticated (用户确认或自动重置)
    /// - Any -> NotAuthenticated (登出)
    /// </remarks>
    public class TokenLifecycleService : ITokenLifecycleService
    {
        private readonly IAuthApi _authApi;
        private readonly ITokenStorageService _tokenStorage;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<TokenLifecycleService> _logger;

        private Timer? _monitorTimer;
        private DateTime? _tokenExpiresAt;
        private TokenLifecycleState _currentState = TokenLifecycleState.NotAuthenticated;
        private readonly object _stateLock = new();
        private bool _disposed;

        /// <summary>
        /// 警告阈值（默认5分钟）
        /// </summary>
        public TimeSpan WarningThreshold { get; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 监控间隔（默认30秒）
        /// </summary>
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(30);

        public TokenLifecycleService(
            IAuthApi authApi,
            ITokenStorageService tokenStorage,
            IEventAggregator eventAggregator,
            ILogger<TokenLifecycleService> logger)
        {
            _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public TokenLifecycleState CurrentState
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentState;
                }
            }
        }

        /// <summary>
        /// Token剩余有效时间
        /// </summary>
        public TimeSpan? RemainingTime
        {
            get
            {
                lock (_stateLock)
                {
                    if (_tokenExpiresAt == null)
                        return null;

                    var remaining = _tokenExpiresAt.Value - DateTime.UtcNow;
                    return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                }
            }
        }

        /// <summary>
        /// 启动生命周期监控
        /// </summary>
        public void StartMonitoring(DateTime tokenExpiresAt)
        {
            lock (_stateLock)
            {
                _tokenExpiresAt = tokenExpiresAt;
                TransitionTo(TokenLifecycleState.Active);

                // 启动定时器
                _monitorTimer?.Dispose();
                _monitorTimer = new Timer(OnMonitorTick, null, _monitorInterval, _monitorInterval);

                _logger.LogInformation("Token生命周期监控已启动 [过期时间: {ExpiresAt}]", tokenExpiresAt);
            }
        }

        /// <summary>
        /// 停止生命周期监控
        /// </summary>
        public void StopMonitoring()
        {
            lock (_stateLock)
            {
                _monitorTimer?.Dispose();
                _monitorTimer = null;
                _logger.LogInformation("Token生命周期监控已停止");
            }
        }

        /// <summary>
        /// 更新Token过期时间（Token刷新后调用）
        /// </summary>
        public void UpdateExpiration(DateTime newExpiresAt)
        {
            lock (_stateLock)
            {
                _tokenExpiresAt = newExpiresAt;

                // 如果之前在Warning状态，刷新成功后回到Active
                if (_currentState == TokenLifecycleState.Warning)
                {
                    TransitionTo(TokenLifecycleState.Active);
                }

                _logger.LogInformation("Token过期时间已更新 [新过期时间: {ExpiresAt}]", newExpiresAt);
            }
        }

        /// <summary>
        /// 尝试刷新Token
        /// </summary>
        public async Task<bool> TryRefreshTokenAsync()
        {
            try
            {
                var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("RefreshToken不存在，无法刷新");
                    return false;
                }

                var request = new RefreshTokenRequest { RefreshToken = refreshToken };
                var response = await _authApi.RefreshTokenAsync(request);

                if (response.Success && response.Data != null)
                {
                    // 更新本地存储（刷新时保持原有登录状态）
                    await _tokenStorage.SaveAuthenticationAsync(response.Data, rememberMe: true);

                    // 更新过期时间
                    UpdateExpiration(response.Data.ExpiresAt);

                    _logger.LogInformation("Token刷新成功");
                    return true;
                }

                _logger.LogWarning("Token刷新失败 [Message: {Message}]", response.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token刷新时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 重置为未认证状态
        /// </summary>
        public void Reset()
        {
            lock (_stateLock)
            {
                StopMonitoring();
                _tokenExpiresAt = null;
                TransitionTo(TokenLifecycleState.NotAuthenticated);
                _logger.LogInformation("Token生命周期已重置");
            }
        }

        /// <summary>
        /// 定时器回调
        /// </summary>
        private void OnMonitorTick(object? state)
        {
            try
            {
                CheckTokenState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token状态检查时发生异常");
            }
        }

        /// <summary>
        /// 检查Token状态
        /// </summary>
        private void CheckTokenState()
        {
            lock (_stateLock)
            {
                if (_tokenExpiresAt == null)
                    return;

                var remaining = _tokenExpiresAt.Value - DateTime.UtcNow;

                if (remaining <= TimeSpan.Zero)
                {
                    // Token已过期
                    if (_currentState != TokenLifecycleState.Expired)
                    {
                        TransitionTo(TokenLifecycleState.Expired);
                    }
                }
                else if (remaining <= WarningThreshold)
                {
                    // 进入警告状态
                    if (_currentState == TokenLifecycleState.Active)
                    {
                        TransitionTo(TokenLifecycleState.Warning);

                        // 尝试自动刷新
                        _ = Task.Run(async () =>
                        {
                            var success = await TryRefreshTokenAsync();
                            if (!success && CurrentState == TokenLifecycleState.Warning)
                            {
                                // 刷新失败，保持Warning状态，让用户决定
                                _logger.LogWarning("自动Token刷新失败，等待用户操作");
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 状态转换
        /// </summary>
        private void TransitionTo(TokenLifecycleState newState)
        {
            var previousState = _currentState;
            if (previousState == newState)
                return;

            _currentState = newState;

            _logger.LogInformation("Token生命周期状态变更 [{Previous} -> {Current}]", previousState, newState);

            // 发布状态变更事件
            var args = new TokenLifecycleStateChangedEventArgs(previousState, newState, RemainingTime);
            _eventAggregator.GetEvent<TokenLifecycleStateChangedEvent>().Publish(args);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _monitorTimer?.Dispose();
            _monitorTimer = null;

            GC.SuppressFinalize(this);
        }
    }
}
