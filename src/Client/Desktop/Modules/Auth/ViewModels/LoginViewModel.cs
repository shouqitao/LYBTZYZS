using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AutoMapper;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 登录视图模型 - UltraThink双层架构UI层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：处理用户登录交互、表单验证、状态管理、导航控制
/// 集成AuthModule双层服务，提供完整的认证用户体验
/// 支持用户名密码登录、记住我功能、API连接检测
/// 适配小型诊所登录流程，确保安全性和易用性
/// </summary>
public class LoginViewModel : ModernViewModelBase
{
    #region 私有字段和依赖注入

    private readonly IAuthService _authModule;
    private readonly IMapper _mapper;
    private LoginRequest _loginRequest = new();
    private string _apiStatus = "正在检测API连接...";

        #endregion

        #region 公共属性

        /// <summary>
        /// 登录命令 - 零警告初始化
        /// </summary>
        public DelegateCommand LoginCommand { get; }

        /// <summary>
        /// 密码变更命令 - 零警告初始化
        /// </summary>
        public DelegateCommand<PasswordBox> PasswordChangedCommand { get; }

        /// <summary>登录请求模型</summary>
        public LoginRequest LoginRequest
        {
            get => _loginRequest;
            set
            {
                if (SetProperty(ref _loginRequest, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>用户名</summary>
        public string Username
        {
            get => LoginRequest.Username;
            set
            {
                if (LoginRequest.Username != value)
                {
                    LoginRequest.Username = value;
                    RaisePropertyChanged(nameof(Username));
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>密码</summary>
        public string Password
        {
            get => LoginRequest.Password;
            set
            {
                if (LoginRequest.Password != value)
                {
                    LoginRequest.Password = value;
                    RaisePropertyChanged(nameof(Password));
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>记住我</summary>
        public bool RememberMe
        {
            get => LoginRequest.RememberMe;
            set
            {
                if (LoginRequest.RememberMe != value)
                {
                    LoginRequest.RememberMe = value;
                    RaisePropertyChanged(nameof(RememberMe));
                }
            }
        }

        /// <summary>是否有保存的密码</summary>
        public bool HasSavedPassword { get; set; } = false;

        /// <summary>API是否在线</summary>
        public bool IsApiOnline { get; set; } = true;

        /// <summary>API状态信息</summary>
        public string ApiStatus
        {
            get => _apiStatus;
            set => SetProperty(ref _apiStatus, value);
        }

        #endregion

    #region 构造函数和初始化

    /// <summary>
    /// 构造函数 - UltraThink双层架构依赖注入
    /// 初始化认证模块、映射器、命令和事件订阅
    /// </summary>
    /// <param name="eventAggregator">事件聚合器</param>
    /// <param name="authModule">认证模块主服务</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="errorHandlingService">错误处理服务</param>
    /// <exception cref="ArgumentNullException">当关键参数为空时抛出</exception>
    public LoginViewModel(
        IEventAggregator eventAggregator,
        IAuthService authModule,
        IMapper mapper,
        IErrorHandlingService? errorHandlingService = null)
        : base(eventAggregator, errorHandlingService)
    {
        _authModule = authModule ?? throw new ArgumentNullException(nameof(authModule));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        // 现代命令初始化 - 避免async void反模式
        LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);
        PasswordChangedCommand = new DelegateCommand<PasswordBox>(OnPasswordChanged);

        // 初始化登录请求配置
        LoginRequest.UserAgent = "LYBT.WPF.Client";
        LoginRequest.LoginType = "Password";

        // 订阅认证相关系统事件
        EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);

        // 启动API连接状态监控
        _ = Task.Run(InitializeApiMonitoringAsync);
    }

    /// <summary>
    /// 异步初始化API连接监控
    /// 后台检查认证服务可用性，更新UI状态
    /// </summary>
    private async Task InitializeApiMonitoringAsync()
    {
        try
        {
            // Simple connectivity check by trying to validate a dummy token
            var connectionResult = false;
            try
            {
                await _authModule.ValidateTokenAsync("dummy");
                connectionResult = true; // If no exception, API is reachable
            }
            catch
            {
                connectionResult = false; // API is not reachable
            }
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsApiOnline = connectionResult;
                ApiStatus = connectionResult ? "API连接正常" : "API连接异常";
                LoginCommand.RaiseCanExecuteChanged();
            });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsApiOnline = false;
                ApiStatus = $"API连接检查失败: {ex.Message}";
                LoginCommand.RaiseCanExecuteChanged();
            });
        }
    }

    #endregion

        #region Command 重写

        /// <summary>
        /// 重写Command状态更新
        /// </summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            LoginCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令处理

        private bool CanExecuteLogin()
        {
            return !IsLoading && IsApiOnline && 
                   !string.IsNullOrWhiteSpace(LoginRequest.Username) && 
                   !string.IsNullOrWhiteSpace(LoginRequest.Password);
        }

        private async Task ExecuteLoginAsync()
        {
            var success = await ExecuteAsync(async () =>
            {
                // UltraThink四层架构：使用模块化服务执行登录
                var result = await _authModule.LoginAsync(LoginRequest);

                if (result.IsSuccess && result.Data != null)
                {
                    // 设置状态消息
                    if (result.Data.User?.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        SetStatus("超级管理员登录成功，正在跳转...");
                    }
                    else
                    {
                        SetStatus("用户登录成功，正在跳转...");
                    }

                    // 等待一下让用户看到成功消息
                    await Task.Delay(1000);

                    // 通过事件总线通知登录成功
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
                }
                else
                {
                    SetError(result.ErrorMessage ?? "登录失败，请检查用户名和密码");
                }
            }, "登录");
        }

        private void OnPasswordChanged(PasswordBox? passwordBox)
        {
            if (passwordBox != null)
            {
                Password = passwordBox.Password;
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 登出事件处理
        /// </summary>
        private void OnLogout()
        {
            ClearError();
            ClearStatus();

            // 清除登录状态
            LoginRequest = new LoginRequest
            {
                UserAgent = "LYBT.WPF.Client",
                LoginType = "Password"
            };

            // 登出时重新加载保存的凭据（如果有）
            LoadSavedCredentials();
        }

        /// <summary>
        /// 认证状态变更事件处理
        /// </summary>
        private void OnAuthStatusChanged(object? sender, (bool IsLoggedIn, string? Username, string? Message) e)
        {
            try
            {
                // 在UI线程上更新状态
                if (Application.Current?.Dispatcher != null)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        UpdateAuthStatus(e);
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateAuthStatus(e)));
                    }
                }
                else
                {
                    UpdateAuthStatus(e);
                }
            }
            catch (Exception ex)
            {
                _ = HandleErrorAsync("认证状态更新", ex, false);
            }
        }

        private void UpdateAuthStatus((bool IsLoggedIn, string? Username, string? Message) e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                if (e.IsLoggedIn)
                {
                    SetStatus(e.Message);
                }
                else
                {
                    SetError(e.Message);
                }
            }
        }

        /// <summary>
        /// API连接状态变更事件处理
        /// </summary>
        private void OnApiConnectionChanged(object? sender, (bool IsConnected, string Message) e)
        {
            try
            {
                // 在UI线程上更新API状态
                if (Application.Current?.Dispatcher != null)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        UpdateApiConnectionStatus(e);
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateApiConnectionStatus(e)));
                    }
                }
                else
                {
                    UpdateApiConnectionStatus(e);
                }
            }
            catch (Exception ex)
            {
                _ = HandleErrorAsync("API状态更新", ex, false);
            }
        }

        private void UpdateApiConnectionStatus((bool IsConnected, string Message) e)
        {
            IsApiOnline = e.IsConnected;
            ApiStatus = e.Message;
            RaisePropertyChanged(nameof(IsApiOnline));
            RaiseCanExecuteChanged();
        }

        #endregion

        #region 凭据管理

        /// <summary>
        /// 加载保存的凭据
        /// </summary>
        private void LoadSavedCredentials()
        {
            // 简化版本：不支持保存凭据功能
            HasSavedPassword = false;
        }

        #endregion

        #region 清理资源

        /// <summary>
        /// 清理资源
        /// </summary>
        protected override void OnDisposing()
        {
            // 取消事件订阅
            if (_authModule != null)
            {
                // 简化版本：无事件订阅需要清理
            }

            base.OnDisposing();
        }

        #endregion
    }
