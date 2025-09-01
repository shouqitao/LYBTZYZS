using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
// UltraThink v2.0: 直接使用shared层API和DTO
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Services;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Auth.Services
{
    /// <summary>
    /// Auth模块业务服务实现 - UltraThink统一架构标准
    /// 直接实现IAuthenticationService，消除双重服务架构
    /// 统一管理认证相关的所有业务逻辑，与其他模块保持一致的直接模式
    /// </summary>
    public class AuthModule : IAuthenticationService, IDisposable
    {
        #region 私有字段

        // UltraThink统一架构：直接使用API和核心服务，移除中间服务层
        private readonly IAuthApi _authApi;
        private readonly ITokenManager _tokenManager;
        private readonly SecureCredentialService _credentialService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthModule> _logger;
        private Timer? _apiCheckTimer;
        private readonly object _lockObject = new();
        private bool _disposed = false;

        // UltraThink统一架构：状态管理和认证信息缓存
        private LoginResponse? _currentLoginResponse;
        private UserDto? _currentUser;
        private bool _isAuthenticated;
        private object _currentApiStatus;
        private bool _isMonitoring;

        #endregion

        #region 事件

        public event EventHandler<(bool IsLoggedIn, string? Username, string? Message)>? AuthStatusChanged;
        public event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;

        #endregion

        #region 构造函数

        public AuthModule(
            IAuthApi authApi,
            ITokenManager tokenManager,
            SecureCredentialService credentialService,
            IMapper mapper,
            ILogger<AuthModule> logger)
        {
            _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // UltraThink v2.0: 使用匿名对象替代ApiStatusInfo
            _currentApiStatus = new
            {
                IsOnline = false,
                StatusMessage = "正在检测API连接...",
                LastCheckTime = DateTime.Now,
                ResponseTime = (TimeSpan?)null
            };

            // 启动API连接监控
            StartApiConnectionMonitoring();
        }

        #endregion

        #region IAuthenticationService 实现

        /// <summary>
        /// 检查是否已登录 - IAuthenticationService接口实现
        /// </summary>
        public bool IsLoggedIn => _isAuthenticated && _currentUser != null;

        /// <summary>
        /// 获取Token - IAuthenticationService接口实现
        /// </summary>
        public string? GetToken()
        {
            return _tokenManager.GetToken();
        }

        /// <summary>
        /// 清除认证信息 - IAuthenticationService接口实现
        /// </summary>
        public void ClearAuthInfo()
        {
            _isAuthenticated = false;
            _currentUser = null;
            _currentLoginResponse = null;
            _tokenManager.ClearToken();
        }

        /// <summary>
        /// 检查API连接状态 - IAuthenticationService接口实现
        /// </summary>
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var response = await _authApi.HealthCheckAsync();
                return !string.IsNullOrEmpty(response);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 登录认证

        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                _logger.LogInformation("开始用户登录: {Username}", loginRequest.Username);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validation = ValidateLoginRequest(loginRequest);
                if (!validation.IsSuccess)
                {
                    return ServiceResult<LoginResponse>.Failure(validation.ErrorMessage ?? "登录信息验证失败");
                }

                // UltraThink统一架构: 直接调用API服务，移除中间服务层
                var apiResponse = await _authApi.LoginAsync(loginRequest);
                
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    var loginResponse = apiResponse.Data;
                    
                    // 更新认证状态和缓存
                    _isAuthenticated = true;
                    _currentUser = loginResponse.User;
                    _currentLoginResponse = loginResponse;
                    _tokenManager.SetToken(loginResponse.Token);

                    // 保存凭据（如果选择了记住我）
                    if (loginRequest.RememberMe)
                    {
                        SaveCredentials(loginRequest.Username, loginRequest.Password, true);
                    }

                    // 触发事件
                    OnAuthStatusChanged(true, loginResponse.User.Username, "登录成功");

                    _logger.LogInformation("用户登录成功: {Username}", loginRequest.Username);
                    return ServiceResult<LoginResponse>.Success(loginResponse);
                }
                else
                {
                    var errorMessage = apiResponse.Message ?? "登录失败，请检查用户名和密码";
                    OnAuthStatusChanged(false, loginRequest.Username, errorMessage);
                    _logger.LogWarning("用户登录失败: {Username}, 错误: {Error}", loginRequest.Username, errorMessage);
                    return ServiceResult<LoginResponse>.Failure(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"登录异常: {ex.Message}";
                OnAuthStatusChanged(false, loginRequest?.Username, errorMessage);
                _logger.LogError(ex, "用户登录异常: {Username}", loginRequest?.Username);
                return ServiceResult<LoginResponse>.Failure(errorMessage);
            }
        }

        public async Task<ServiceResult> LogoutAsync()
        {
            try
            {
                _logger.LogInformation("开始用户登出");

                // UltraThink统一架构: 尝试调用服务器登出（失败不影响本地清理）
                try
                {
                    await _authApi.LogoutAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "服务器登出失败，继续本地清理");
                }
                
                // 清除本地状态和缓存
                ClearAuthInfo();
                
                // UltraThink v2.0: 清除模块状态
                _currentLoginResponse = null;

                // 触发事件
                OnAuthStatusChanged(false, null, "已登出");

                _logger.LogInformation("用户登出完成");
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出异常");
                return ServiceResult.Failure($"登出异常: {ex.Message}");
            }
        }

        // IsLoggedIn 属性已在 IAuthenticationService 实现区域定义

        /// <summary>
        /// 获取当前用户信息 - IAuthenticationService接口实现
        /// </summary>
        public Task<UserDto?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }
        
        /// <summary>
        /// 获取当前用户信息（带ServiceResult包装） - 业务模块方法
        /// </summary>
        public async Task<ServiceResult<UserDto?>> GetCurrentUserWithResultAsync()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                return ServiceResult<UserDto?>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前用户信息异常");
                return ServiceResult<UserDto?>.Failure($"获取用户信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync()
        {
            try
            {
                _logger.LogInformation("开始刷新Token");
                
                // TODO: 实现Token刷新逻辑
                // 这里需要根据后端API实现Token刷新机制
                
                await Task.CompletedTask;
                return ServiceResult<LoginResponse>.Failure(string.Format(SystemConstants.FeaturePendingTemplate, "Token刷新"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token异常");
                return ServiceResult<LoginResponse>.Failure($"刷新Token失败: {ex.Message}");
            }
        }

        #endregion

        #region 会话管理

        public async Task<ServiceResult<bool>> ValidateTokenAsync()
        {
            try
            {
                // 检查Token是否存在
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<bool>.Success(false);
                }

                // 尝试获取当前用户来验证Token有效性
                var user = await GetCurrentUserAsync();
                return ServiceResult<bool>.Success(user != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证Token异常");
                return ServiceResult<bool>.Failure($"验证Token失败: {ex.Message}");
            }
        }

        public int GetSessionRemainingMinutes()
        {
            try
            {
                // TODO: 实现会话过期时间计算
                // 这里需要根据Token的过期时间计算剩余时间
                return 480; // 默认8小时
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话剩余时间异常");
                return 0;
            }
        }

        #endregion

        #region 凭据管理

        public ServiceResult SaveCredentials(string username, string password, bool rememberMe)
        {
            try
            {
                _credentialService.SaveCredentials(username, password, rememberMe);
                _logger.LogInformation("保存用户凭据成功: {Username}", username);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户凭据异常: {Username}", username);
                return ServiceResult.Failure($"保存凭据失败: {ex.Message}");
            }
        }

        public ServiceResult<LoginRequest?> LoadSavedCredentials()
        {
            try
            {
                var savedCredentials = _credentialService.LoadCredentials();
                if (savedCredentials != null)
                {
                    // UltraThink v2.0: 创建LoginRequest对象
                    var loginRequest = new LoginRequest
                    {
                        Username = savedCredentials.Username,
                        Password = savedCredentials.Password,
                        RememberMe = savedCredentials.RememberMe
                    };

                    _logger.LogInformation("加载保存的凭据成功: {Username}", savedCredentials.Username);
                    return ServiceResult<LoginRequest?>.Success(loginRequest);
                }

                return ServiceResult<LoginRequest?>.Success(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载保存的凭据异常");
                return ServiceResult<LoginRequest?>.Failure($"加载凭据失败: {ex.Message}");
            }
        }

        public ServiceResult ClearSavedCredentials()
        {
            try
            {
                _credentialService.ClearCredentials();
                _logger.LogInformation("清除保存的凭据成功");
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除保存的凭据异常");
                return ServiceResult.Failure($"清除凭据失败: {ex.Message}");
            }
        }

        public bool HasSavedCredentials()
        {
            try
            {
                var credentials = _credentialService.LoadCredentials();
                return credentials != null && !string.IsNullOrEmpty(credentials.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查保存的凭据异常");
                return false;
            }
        }

        #endregion

        #region 系统连接

        public async Task<ServiceResult<bool>> CheckApiConnectionAsync()
        {
            try
            {
                var startTime = DateTime.Now;
                var isOnline = await CheckConnectionAsync();
                var responseTime = DateTime.Now - startTime;

                lock (_lockObject)
                {
                    // UltraThink v2.0: 使用dynamic访问旧状态
                    var oldStatus = ((dynamic)_currentApiStatus).IsOnline;
                    _currentApiStatus = new
                    {
                        IsOnline = isOnline,
                        StatusMessage = isOnline ? "✅ API连接正常" : "❌ API服务不可用",
                        LastCheckTime = DateTime.Now,
                        ResponseTime = responseTime
                    };

                    // 如果状态发生变化，触发事件
                    if (oldStatus != isOnline)
                    {
                        OnApiConnectionChanged(isOnline, ((dynamic)_currentApiStatus).StatusMessage);
                    }
                }

                return ServiceResult<bool>.Success(isOnline);
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ 连接失败: {ex.Message}";
                
                lock (_lockObject)
                {
                    // UltraThink v2.0: 使用dynamic访问旧状态
                    var oldStatus = ((dynamic)_currentApiStatus).IsOnline;
                    _currentApiStatus = new
                    {
                        IsOnline = false,
                        StatusMessage = errorMessage,
                        LastCheckTime = DateTime.Now,
                        ResponseTime = (TimeSpan?)null
                    };

                    if (oldStatus)
                    {
                        OnApiConnectionChanged(false, errorMessage);
                    }
                }

                _logger.LogError(ex, "检查API连接异常");
                return ServiceResult<bool>.Success(false);
            }
        }

        public ServiceResult<object> GetApiStatus()
        {
            lock (_lockObject)
            {
                return ServiceResult<object>.Success(_currentApiStatus);
            }
        }

        public void StartApiConnectionMonitoring()
        {
            if (_isMonitoring) return;

            lock (_lockObject)
            {
                if (_isMonitoring) return;

                _isMonitoring = true;
                
                // 立即执行一次检测
                _ = Task.Run(async () => await CheckApiConnectionAsync());

                // 设置定时器，每5秒检测一次
                _apiCheckTimer = new Timer(
                    async _ => await CheckApiConnectionAsync(),
                    null,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5));

                _logger.LogInformation("API连接监控已启动");
            }
        }

        public void StopApiConnectionMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring) return;

                _isMonitoring = false;
                _apiCheckTimer?.Dispose();
                _apiCheckTimer = null;

                _logger.LogInformation("API连接监控已停止");
            }
        }

        #endregion

        #region 安全功能

        // UltraThink v2.0: 新增DTO验证方法
        public ServiceResult ValidateLoginRequest(LoginRequest loginRequest)
        {
            try
            {
                if (loginRequest == null)
                {
                    return ServiceResult.Failure("登录信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(loginRequest.Username))
                {
                    return ServiceResult.Failure("用户名不能为空");
                }

                if (loginRequest.Username.Length < 3 || loginRequest.Username.Length > 32)
                {
                    return ServiceResult.Failure("用户名长度必须在3到32个字符之间");
                }

                if (string.IsNullOrWhiteSpace(loginRequest.Password))
                {
                    return ServiceResult.Failure("密码不能为空");
                }

                if (loginRequest.Password.Length < 6)
                {
                    return ServiceResult.Failure("密码长度不能少于6个字符");
                }

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证登录信息异常");
                return ServiceResult.Failure($"验证异常: {ex.Message}");
            }
        }

        // UltraThink v2.0: 移除过度设计的安全功能 - IP地址获取、设备指纹、账户锁定检查

        #endregion

        #region 密码管理

        // UltraThink v2.0: 移除密码管理功能 - 删除过度设计的密码修改、重置、强度验证功能

        #endregion

        // UltraThink v2.0: 移除多因子认证功能 - 删除过度设计的预留功能

        #region 私有方法

        private void OnAuthStatusChanged(bool isLoggedIn, string? username, string? statusMessage)
        {
            try
            {
                AuthStatusChanged?.Invoke(this, (isLoggedIn, username, statusMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "触发认证状态变更事件异常");
            }
        }

        private void OnApiConnectionChanged(bool isConnected, string statusMessage)
        {
            try
            {
                ApiConnectionChanged?.Invoke(this, (isConnected, statusMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "触发API连接状态变更事件异常");
            }
        }

        #endregion

        // UltraThink v2.0: 移除私有辅助方法 - 删除过度设计的密码强度等级映射

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            StopApiConnectionMonitoring();
            _disposed = true;
        }

        #endregion
    }
}