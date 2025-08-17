using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Auth.Services.Interfaces;
using LYBT.Desktop.Core.Models.Auth;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Services;

namespace LYBT.Desktop.Auth.Services
{
    /// <summary>
    /// Auth模块业务服务实现 - UltraThink四层架构标准
    /// 统一管理认证相关的所有业务逻辑，封装底层服务调用
    /// </summary>
    public class AuthModuleService : IAuthModuleService, IDisposable
    {
        #region 私有字段

        private readonly IAuthenticationService _authenticationService;
        private readonly ICredentialService _credentialService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthModuleService> _logger;
        private Timer? _apiCheckTimer;
        private readonly object _lockObject = new();
        private bool _disposed = false;

        // 状态缓存
        private LoginInfo? _currentLoginInfo;
        private ApiStatusInfo _currentApiStatus;
        private bool _isMonitoring;

        #endregion

        #region 事件

        public event EventHandler<AuthStatusChangedEventArgs>? AuthStatusChanged;
        public event EventHandler<ApiConnectionChangedEventArgs>? ApiConnectionChanged;

        #endregion

        #region 构造函数

        public AuthModuleService(
            IAuthenticationService authenticationService,
            ICredentialService credentialService,
            IMapper mapper,
            ILogger<AuthModuleService> logger)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _currentApiStatus = new ApiStatusInfo
            {
                IsOnline = false,
                StatusMessage = "正在检测API连接...",
                LastCheckTime = DateTime.Now
            };

            // 启动API连接监控
            StartApiConnectionMonitoring();
        }

        #endregion

        #region 登录认证

        public async Task<ServiceResult<LoginInfo>> LoginAsync(LoginInfo loginInfo)
        {
            try
            {
                _logger.LogInformation("开始用户登录: {Username}", loginInfo.Username);

                // 1. 验证登录信息
                var validation = ValidateLoginInfo(loginInfo);
                if (!validation.IsSuccess)
                {
                    return ServiceResult<LoginInfo>.Failure(validation.ErrorMessage ?? "登录信息验证失败");
                }

                // 2. 设置登录状态
                loginInfo.IsLoggingIn = true;
                loginInfo.ErrorMessage = null;

                // 3. 转换为DTO并调用底层服务
                var request = _mapper.Map<LoginRequest>(loginInfo);
                var response = await _authenticationService.LoginAsync(request);

                if (response.IsSuccess && response.Data != null)
                {
                    // 4. 使用AutoMapper合并响应到LoginInfo
                    var updatedLoginInfo = _mapper.Map<(LoginInfo, LoginResponse), LoginInfo>((loginInfo, response.Data));
                    updatedLoginInfo.IsLoggingIn = false;
                    updatedLoginInfo.ErrorMessage = null;
                    updatedLoginInfo.StatusMessage = "登录成功";

                    // 5. 保存到缓存
                    _currentLoginInfo = updatedLoginInfo;

                    // 6. 保存凭据（如果选择了记住我）
                    if (loginInfo.RememberMe)
                    {
                        SaveCredentials(loginInfo.Username, loginInfo.Password, true);
                    }

                    // 7. 触发事件
                    OnAuthStatusChanged(true, updatedLoginInfo.Username, "登录成功");

                    _logger.LogInformation("用户登录成功: {Username}", loginInfo.Username);
                    return ServiceResult<LoginInfo>.Success(updatedLoginInfo, "登录成功");
                }
                else
                {
                    var errorMessage = response.ErrorMessage ?? "登录失败，请检查用户名和密码";
                    loginInfo.SetLoginFailure(errorMessage);
                    OnAuthStatusChanged(false, loginInfo.Username, errorMessage);
                    _logger.LogWarning("用户登录失败: {Username}, 错误: {Error}", loginInfo.Username, errorMessage);
                    return ServiceResult<LoginInfo>.Failure(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"登录异常: {ex.Message}";
                loginInfo?.SetLoginFailure(errorMessage);
                OnAuthStatusChanged(false, loginInfo?.Username, errorMessage);
                _logger.LogError(ex, "用户登录异常: {Username}", loginInfo?.Username);
                return ServiceResult<LoginInfo>.Failure(errorMessage);
            }
            finally
            {
                if (loginInfo != null)
                {
                    loginInfo.IsLoggingIn = false;
                }
            }
        }

        public async Task<ServiceResult> LogoutAsync()
        {
            try
            {
                _logger.LogInformation("开始用户登出");

                var result = await _authenticationService.LogoutAsync();
                
                // 清除本地状态
                _currentLoginInfo?.ClearLoginState();
                _currentLoginInfo = null;

                // 触发事件
                OnAuthStatusChanged(false, null, "已登出");

                _logger.LogInformation("用户登出完成");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出异常");
                return ServiceResult.Failure($"登出异常: {ex.Message}");
            }
        }

        public bool IsLoggedIn => _authenticationService.IsLoggedIn;

        public async Task<ServiceResult<LoginInfo?>> GetCurrentUserAsync()
        {
            try
            {
                if (_currentLoginInfo != null && _currentLoginInfo.IsLoggedIn)
                {
                    return ServiceResult<LoginInfo?>.Success(_currentLoginInfo);
                }

                var userInfo = await _authenticationService.GetCurrentUserAsync();
                if (userInfo != null)
                {
                    // 转换为LoginInfo
                    var loginInfo = new LoginInfo
                    {
                        User = _mapper.Map<LYBT.Shared.Models.Core.BaseUser>(userInfo),
                        IsLoggedIn = true,
                        Token = _authenticationService.GetToken() ?? string.Empty
                    };
                    
                    _currentLoginInfo = loginInfo;
                    return ServiceResult<LoginInfo?>.Success(loginInfo);
                }

                return ServiceResult<LoginInfo?>.Success(null, "未登录");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前用户信息异常");
                return ServiceResult<LoginInfo?>.Failure($"获取用户信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<LoginInfo>> RefreshTokenAsync()
        {
            try
            {
                _logger.LogInformation("开始刷新Token");
                
                // TODO: 实现Token刷新逻辑
                // 这里需要根据后端API实现Token刷新机制
                
                await Task.CompletedTask;
                return ServiceResult<LoginInfo>.Failure("Token刷新功能尚未实现");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token异常");
                return ServiceResult<LoginInfo>.Failure($"刷新Token失败: {ex.Message}");
            }
        }

        #endregion

        #region 会话管理

        public string? GetToken()
        {
            return _authenticationService.GetToken();
        }

        public async Task<ServiceResult<bool>> ValidateTokenAsync()
        {
            try
            {
                // 检查Token是否存在
                var token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<bool>.Success(false, "Token不存在");
                }

                // 尝试获取当前用户来验证Token有效性
                var result = await GetCurrentUserAsync();
                return ServiceResult<bool>.Success(result.IsSuccess, result.IsSuccess ? "Token有效" : "Token无效");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证Token异常");
                return ServiceResult<bool>.Failure($"验证Token失败: {ex.Message}");
            }
        }

        public void ClearAuthInfo()
        {
            _authenticationService.ClearAuthInfo();
            _currentLoginInfo?.ClearLoginState();
            _currentLoginInfo = null;
            OnAuthStatusChanged(false, null, "认证信息已清除");
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
                return ServiceResult.Success("凭据保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户凭据异常: {Username}", username);
                return ServiceResult.Failure($"保存凭据失败: {ex.Message}");
            }
        }

        public ServiceResult<LoginInfo?> LoadSavedCredentials()
        {
            try
            {
                var savedCredentials = _credentialService.LoadCredentials();
                if (savedCredentials != null)
                {
                    var loginInfo = new LoginInfo
                    {
                        Username = savedCredentials.Username,
                        Password = savedCredentials.Password,
                        RememberMe = savedCredentials.RememberMe,
                        HasSavedPassword = !string.IsNullOrEmpty(savedCredentials.Password),
                        UserAgent = "LYBT.WPF.Client",
                        LoginType = "Password",
                        ClientIp = GetClientIpAddress()
                    };

                    _logger.LogInformation("加载保存的凭据成功: {Username}", savedCredentials.Username);
                    return ServiceResult<LoginInfo?>.Success(loginInfo, "加载凭据成功");
                }

                return ServiceResult<LoginInfo?>.Success(null, "无保存的凭据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载保存的凭据异常");
                return ServiceResult<LoginInfo?>.Failure($"加载凭据失败: {ex.Message}");
            }
        }

        public ServiceResult ClearSavedCredentials()
        {
            try
            {
                _credentialService.ClearCredentials();
                _logger.LogInformation("清除保存的凭据成功");
                return ServiceResult.Success("凭据清除成功");
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
                var isOnline = await _authenticationService.CheckConnectionAsync();
                var responseTime = DateTime.Now - startTime;

                lock (_lockObject)
                {
                    var oldStatus = _currentApiStatus.IsOnline;
                    _currentApiStatus = new ApiStatusInfo
                    {
                        IsOnline = isOnline,
                        StatusMessage = isOnline ? "✅ API连接正常" : "❌ API服务不可用",
                        LastCheckTime = DateTime.Now,
                        ResponseTime = responseTime
                    };

                    // 如果状态发生变化，触发事件
                    if (oldStatus != isOnline)
                    {
                        OnApiConnectionChanged(isOnline, _currentApiStatus.StatusMessage);
                    }
                }

                return ServiceResult<bool>.Success(isOnline, _currentApiStatus.StatusMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ 连接失败: {ex.Message}";
                
                lock (_lockObject)
                {
                    var oldStatus = _currentApiStatus.IsOnline;
                    _currentApiStatus = new ApiStatusInfo
                    {
                        IsOnline = false,
                        StatusMessage = errorMessage,
                        LastCheckTime = DateTime.Now
                    };

                    if (oldStatus)
                    {
                        OnApiConnectionChanged(false, errorMessage);
                    }
                }

                _logger.LogError(ex, "检查API连接异常");
                return ServiceResult<bool>.Success(false, errorMessage);
            }
        }

        public ServiceResult<ApiStatusInfo> GetApiStatus()
        {
            lock (_lockObject)
            {
                return ServiceResult<ApiStatusInfo>.Success(_currentApiStatus);
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

        public ServiceResult ValidateLoginInfo(LoginInfo loginInfo)
        {
            try
            {
                var (isValid, errorMessage) = loginInfo.Validate();
                return isValid ? ServiceResult.Success("验证通过") : ServiceResult.Failure(errorMessage ?? "验证失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证登录信息异常");
                return ServiceResult.Failure($"验证异常: {ex.Message}");
            }
        }

        public string GetClientIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户端IP地址异常");
            }
            return "127.0.0.1";
        }

        public string GenerateDeviceFingerprint()
        {
            try
            {
                var deviceInfo = $"{Environment.MachineName}_{Environment.UserName}_{Environment.OSVersion}";
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(deviceInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成设备指纹异常");
                return "unknown_device";
            }
        }

        public async Task<ServiceResult<AccountLockInfo>> CheckAccountLockStatusAsync(string username)
        {
            try
            {
                // TODO: 实现账户锁定状态检查
                // 这里需要调用后端API检查账户锁定状态
                await Task.CompletedTask;
                
                var lockInfo = new AccountLockInfo
                {
                    IsLocked = false,
                    FailedAttempts = 0,
                    MaxAttempts = 5
                };

                return ServiceResult<AccountLockInfo>.Success(lockInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查账户锁定状态异常: {Username}", username);
                return ServiceResult<AccountLockInfo>.Failure($"检查锁定状态失败: {ex.Message}");
            }
        }

        #endregion

        #region 密码管理

        public async Task<ServiceResult> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                // TODO: 实现密码修改功能
                // 这里需要调用后端API修改密码
                await Task.CompletedTask;
                
                _logger.LogInformation("修改密码功能调用");
                return ServiceResult.Failure("密码修改功能尚未实现");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密码异常");
                return ServiceResult.Failure($"修改密码失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> RequestPasswordResetAsync(string username, string email)
        {
            try
            {
                // TODO: 实现密码重置功能
                // 这里需要调用后端API发送重置邮件
                await Task.CompletedTask;
                
                _logger.LogInformation("密码重置请求: {Username}, {Email}", username, email);
                return ServiceResult.Failure("密码重置功能尚未实现");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求密码重置异常: {Username}", username);
                return ServiceResult.Failure($"密码重置失败: {ex.Message}");
            }
        }

        public ServiceResult<PasswordStrengthInfo> ValidatePasswordStrength(string password)
        {
            try
            {
                var strengthInfo = new PasswordStrengthInfo();
                var suggestions = new List<string>();

                // 计算密码强度分数
                int score = 0;
                
                if (password.Length >= 8) score += 1;
                else suggestions.Add("密码长度至少8位");

                if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]")) score += 1;
                else suggestions.Add("包含小写字母");

                if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")) score += 1;
                else suggestions.Add("包含大写字母");

                if (System.Text.RegularExpressions.Regex.IsMatch(password, @"\d")) score += 1;
                else suggestions.Add("包含数字");

                if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*]")) score += 1;
                else suggestions.Add("包含特殊字符");

                strengthInfo.Score = score;
                strengthInfo.Level = (PasswordStrengthLevel)Math.Min(score, 5);
                strengthInfo.Suggestions = suggestions;
                strengthInfo.MeetsPolicy = score >= 3;

                return ServiceResult<PasswordStrengthInfo>.Success(strengthInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码强度异常");
                return ServiceResult<PasswordStrengthInfo>.Failure($"验证密码强度失败: {ex.Message}");
            }
        }

        #endregion

        #region 多因子认证（预留）

        public async Task<ServiceResult> SendVerificationCodeAsync(string phoneNumber)
        {
            try
            {
                // TODO: 实现验证码发送功能
                await Task.CompletedTask;
                
                _logger.LogInformation("发送验证码: {PhoneNumber}", phoneNumber);
                return ServiceResult.Failure("验证码功能尚未实现");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送验证码异常: {PhoneNumber}", phoneNumber);
                return ServiceResult.Failure($"发送验证码失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> VerifyCodeAsync(string phoneNumber, string code)
        {
            try
            {
                // TODO: 实现验证码验证功能
                await Task.CompletedTask;
                
                _logger.LogInformation("验证验证码: {PhoneNumber}", phoneNumber);
                return ServiceResult<bool>.Failure("验证码功能尚未实现");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证验证码异常: {PhoneNumber}", phoneNumber);
                return ServiceResult<bool>.Failure($"验证验证码失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        private void OnAuthStatusChanged(bool isLoggedIn, string? username, string? statusMessage)
        {
            try
            {
                AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(isLoggedIn, username, statusMessage));
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
                ApiConnectionChanged?.Invoke(this, new ApiConnectionChangedEventArgs(isConnected, statusMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "触发API连接状态变更事件异常");
            }
        }

        #endregion

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