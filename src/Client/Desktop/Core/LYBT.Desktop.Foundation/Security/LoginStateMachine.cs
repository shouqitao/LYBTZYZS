using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 登录状态机实现
    /// OpenSpec: refactor-login-authentication (Phase 2.1, 3.2)
    /// OpenSpec: unify-event-system (Phase 2.2)
    /// 管理登录流程的状态转换，使用状态转换表确保转换合法性
    /// 通过Prism EventAggregator发布状态变更事件
    /// </summary>
    public class LoginStateMachine : ILoginStateMachine
    {
        private readonly ILogger<LoginStateMachine> _logger;
        private readonly IEventAggregator? _eventAggregator;
        private readonly object _stateLock = new();
        private LoginState _currentState = LoginState.NotLoggedIn;

        // 状态转换表: (当前状态, 触发器) -> 目标状态
        private static readonly Dictionary<(LoginState, LoginTrigger), LoginState> Transitions = new()
        {
            // NotLoggedIn 状态的转换
            { (LoginState.NotLoggedIn, LoginTrigger.StartLogin), LoginState.LoggingIn },
            { (LoginState.NotLoggedIn, LoginTrigger.StartAutoLogin), LoginState.AutoLoggingIn },

            // LoggingIn 状态的转换
            { (LoginState.LoggingIn, LoginTrigger.LoginSuccess), LoginState.LoggedIn },
            { (LoginState.LoggingIn, LoginTrigger.LoginFailure), LoginState.LoginFailed },
            { (LoginState.LoggingIn, LoginTrigger.Reset), LoginState.NotLoggedIn },

            // AutoLoggingIn 状态的转换
            { (LoginState.AutoLoggingIn, LoginTrigger.LoginSuccess), LoginState.LoggedIn },
            { (LoginState.AutoLoggingIn, LoginTrigger.LoginFailure), LoginState.NotLoggedIn },
            { (LoginState.AutoLoggingIn, LoginTrigger.Reset), LoginState.NotLoggedIn },

            // LoggedIn 状态的转换
            { (LoginState.LoggedIn, LoginTrigger.StartLogout), LoginState.LoggingOut },
            { (LoginState.LoggedIn, LoginTrigger.SessionExpire), LoginState.SessionExpired },
            { (LoginState.LoggedIn, LoginTrigger.StartTokenRefresh), LoginState.TokenRefreshing },

            // LoginFailed 状态的转换
            { (LoginState.LoginFailed, LoginTrigger.StartLogin), LoginState.LoggingIn },
            { (LoginState.LoginFailed, LoginTrigger.Reset), LoginState.NotLoggedIn },

            // LoggingOut 状态的转换
            { (LoginState.LoggingOut, LoginTrigger.LogoutSuccess), LoginState.NotLoggedIn },
            { (LoginState.LoggingOut, LoginTrigger.LogoutFailure), LoginState.LoggedIn },
            { (LoginState.LoggingOut, LoginTrigger.Reset), LoginState.NotLoggedIn },

            // SessionExpired 状态的转换
            { (LoginState.SessionExpired, LoginTrigger.StartLogin), LoginState.LoggingIn },
            { (LoginState.SessionExpired, LoginTrigger.Reset), LoginState.NotLoggedIn },

            // TokenRefreshing 状态的转换
            { (LoginState.TokenRefreshing, LoginTrigger.TokenRefreshSuccess), LoginState.LoggedIn },
            { (LoginState.TokenRefreshing, LoginTrigger.TokenRefreshFailure), LoginState.SessionExpired },
            { (LoginState.TokenRefreshing, LoginTrigger.Reset), LoginState.NotLoggedIn },
        };

        public LoginState CurrentState
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentState;
                }
            }
        }

        public bool IsLoggedIn => CurrentState == LoginState.LoggedIn;

        public bool IsTransitioning => CurrentState is LoginState.LoggingIn
            or LoginState.AutoLoggingIn
            or LoginState.LoggingOut
            or LoginState.TokenRefreshing;

        public LoginStateMachine(ILogger<LoginStateMachine> logger, IEventAggregator? eventAggregator = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// 用于测试的构造函数，允许设置初始状态
        /// </summary>
        internal LoginStateMachine(ILogger<LoginStateMachine> logger, LoginState initialState)
            : this(logger, null)
        {
            _currentState = initialState;
        }

        public bool CanFire(LoginTrigger trigger)
        {
            lock (_stateLock)
            {
                return Transitions.ContainsKey((_currentState, trigger));
            }
        }

        public bool Fire(LoginTrigger trigger)
        {
            lock (_stateLock)
            {
                var key = (_currentState, trigger);
                if (!Transitions.TryGetValue(key, out var newState))
                {
                    _logger.LogWarning("无效的状态转换 [当前状态: {CurrentState}] [触发器: {Trigger}]",
                        _currentState, trigger);
                    return false;
                }

                var previousState = _currentState;
                _currentState = newState;

                _logger.LogInformation("状态转换 [{PreviousState}] --({Trigger})--> [{NewState}]",
                    previousState, trigger, newState);

                // 在锁外触发事件，避免死锁
                var args = new LoginStateChangedEventArgs(previousState, newState, trigger);
                Task.Run(() => RaiseStateChanged(args));

                return true;
            }
        }

        public void Reset()
        {
            Fire(LoginTrigger.Reset);
        }

        private void RaiseStateChanged(LoginStateChangedEventArgs args)
        {
            try
            {
                // 发布Prism PubSubEvent
                if (_eventAggregator != null)
                {
                    var payload = new LoginStateChangedPayload
                    {
                        PreviousState = args.PreviousState,
                        CurrentState = args.CurrentState,
                        Trigger = args.Trigger
                    };
                    _eventAggregator.GetEvent<AuthEvents.LoginStateChangedEvent>().Publish(payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "状态变更事件处理异常 [{PreviousState}] -> [{CurrentState}]",
                    args.PreviousState, args.CurrentState);
            }
        }

        /// <summary>
        /// 获取当前状态允许的所有触发器
        /// </summary>
        public IEnumerable<LoginTrigger> GetPermittedTriggers()
        {
            lock (_stateLock)
            {
                return Transitions.Keys
                    .Where(k => k.Item1 == _currentState)
                    .Select(k => k.Item2)
                    .ToList();
            }
        }

        /// <summary>
        /// 强制设置状态（仅用于恢复场景，跳过转换验证）
        /// </summary>
        internal void ForceState(LoginState state)
        {
            lock (_stateLock)
            {
                var previousState = _currentState;
                _currentState = state;
                _logger.LogWarning("强制状态设置 [{PreviousState}] -> [{NewState}]",
                    previousState, state);
            }
        }
    }
}
