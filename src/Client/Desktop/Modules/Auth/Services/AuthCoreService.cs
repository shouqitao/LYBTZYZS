using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 认证核心服务实现 - UltraThink三层架构核心操作层
/// 职责：API通信、基础认证操作、Token管理、数据验证
/// </summary>
public class AuthCoreService : IAuthCoreService
{
    private readonly IAuthApi _authApi;
    private readonly ITokenManager _tokenManager;
    private readonly ILogger<AuthCoreService> _logger;
    
    // 认证状态缓存
    private bool _isAuthenticated;
    private UserDto? _currentUser;
    private LoginResponse? _currentLoginResponse;
    private readonly object _stateLock = new();
    
    public AuthCoreService(
        IAuthApi authApi,
        ITokenManager tokenManager,
        ILogger<AuthCoreService> logger)
    {
        _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #region API通信操作
    
    public async Task<ServiceResult<LoginResponse>> CallLoginApiAsync(LoginRequest loginRequest)
    {
        try
        {
            _logger.LogInformation("调用登录API: {Username}", loginRequest.Username);
            
            var apiResponse = await _authApi.LoginAsync(loginRequest);
            
            if (apiResponse.Success && apiResponse.Data != null)
            {
                _logger.LogInformation("登录API调用成功: {Username}", loginRequest.Username);
                return ServiceResult<LoginResponse>.Success(apiResponse.Data);
            }
            else
            {
                var errorMessage = apiResponse.Message ?? "登录API调用失败";
                _logger.LogWarning("登录API调用失败: {Username}, 错误: {Error}", loginRequest.Username, errorMessage);
                return ServiceResult<LoginResponse>.Failure(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用登录API异常: {Username}", loginRequest.Username);
            return ServiceResult<LoginResponse>.Failure($"登录API调用异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult> CallLogoutApiAsync()
    {
        try
        {
            _logger.LogInformation("调用登出API");
            
            await _authApi.LogoutAsync();
            
            _logger.LogInformation("登出API调用成功");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调用登出API异常，继续本地清理");
            return ServiceResult.Success(); // 登出失败不影响本地清理
        }
    }
    
    public async Task<ServiceResult<LoginResponse>> CallRefreshTokenApiAsync()
    {
        try
        {
            _logger.LogInformation("调用Token刷新API");
            
            // TODO: 实现Token刷新API调用
            // 这里需要根据后端API实现Token刷新机制
            await Task.CompletedTask;
            
            return ServiceResult<LoginResponse>.Failure("Token刷新功能待实现");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用Token刷新API异常");
            return ServiceResult<LoginResponse>.Failure($"Token刷新API调用异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<bool>> CheckApiHealthAsync()
    {
        try
        {
            var response = await _authApi.HealthCheckAsync();
            var isHealthy = !string.IsNullOrEmpty(response);
            
            _logger.LogDebug("API健康检查结果: {IsHealthy}", isHealthy);
            return ServiceResult<bool>.Success(isHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API健康检查异常");
            return ServiceResult<bool>.Success(false); // 异常时视为不健康，但不返回错误
        }
    }
    
    #endregion
    
    #region Token管理操作
    
    public string? GetToken()
    {
        return _tokenManager.GetToken();
    }
    
    public void SetToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("尝试设置空或无效的Token");
            return;
        }
        
        _tokenManager.SetToken(token);
        _logger.LogInformation("Token已设置");
    }
    
    public void ClearToken()
    {
        _tokenManager.ClearToken();
        _logger.LogInformation("Token已清除");
    }
    
    public ServiceResult ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult.Failure("Token不能为空");
        }
        
        // TODO: 实现Token格式验证（JWT格式检查等）
        if (token.Length < 10)
        {
            return ServiceResult.Failure("Token格式无效");
        }
        
        return ServiceResult.Success();
    }
    
    #endregion
    
    #region 基础数据验证
    
    public ServiceResult ValidateLoginRequest(LoginRequest loginRequest)
    {
        if (loginRequest == null)
        {
            return ServiceResult.Failure("登录信息不能为空");
        }
        
        var usernameValidation = ValidateUsername(loginRequest.Username);
        if (!usernameValidation.IsSuccess)
        {
            return usernameValidation;
        }
        
        var passwordValidation = ValidatePassword(loginRequest.Password);
        if (!passwordValidation.IsSuccess)
        {
            return passwordValidation;
        }
        
        return ServiceResult.Success();
    }
    
    public ServiceResult ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ServiceResult.Failure("用户名不能为空");
        }
        
        if (username.Length < 3 || username.Length > 32)
        {
            return ServiceResult.Failure("用户名长度必须在3到32个字符之间");
        }
        
        // TODO: 添加更多用户名格式验证规则
        
        return ServiceResult.Success();
    }
    
    public ServiceResult ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return ServiceResult.Failure("密码不能为空");
        }
        
        if (password.Length < 6)
        {
            return ServiceResult.Failure("密码长度不能少于6个字符");
        }
        
        // TODO: 添加更多密码强度验证规则
        
        return ServiceResult.Success();
    }
    
    public ServiceResult<bool> ValidateAuthenticationState(UserDto? user, string? token)
    {
        if (user == null)
        {
            return ServiceResult<bool>.Success(false);
        }
        
        if (string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult<bool>.Success(false);
        }
        
        var tokenValidation = ValidateToken(token);
        if (!tokenValidation.IsSuccess)
        {
            return ServiceResult<bool>.Success(false);
        }
        
        return ServiceResult<bool>.Success(true);
    }
    
    #endregion
    
    #region 认证状态管理
    
    public void UpdateAuthenticationState(bool isAuthenticated, UserDto? user, LoginResponse? loginResponse)
    {
        lock (_stateLock)
        {
            _isAuthenticated = isAuthenticated;
            _currentUser = user;
            _currentLoginResponse = loginResponse;
        }
        
        _logger.LogInformation("认证状态已更新: IsAuthenticated={IsAuthenticated}, User={Username}", 
            isAuthenticated, user?.Username ?? "N/A");
    }
    
    public void ClearAuthenticationState()
    {
        lock (_stateLock)
        {
            _isAuthenticated = false;
            _currentUser = null;
            _currentLoginResponse = null;
        }
        
        _logger.LogInformation("认证状态已清除");
    }
    
    public ServiceResult<(bool IsAuthenticated, UserDto? User, LoginResponse? LoginResponse)> GetAuthenticationState()
    {
        lock (_stateLock)
        {
            return ServiceResult<(bool, UserDto?, LoginResponse?)>.Success(
                (_isAuthenticated, _currentUser, _currentLoginResponse));
        }
    }
    
    #endregion
    
    #region 缓存和性能优化
    
    public async Task<ServiceResult> PreWarmAuthCacheAsync()
    {
        try
        {
            _logger.LogInformation("开始预热认证缓存");
            
            // 预热Token验证
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                ValidateToken(token);
            }
            
            // 预热API连接
            await CheckApiHealthAsync();
            
            _logger.LogInformation("认证缓存预热完成");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预热认证缓存异常");
            return ServiceResult.Failure($"预热认证缓存异常: {ex.Message}");
        }
    }
    
    public ServiceResult ClearAuthCache()
    {
        try
        {
            ClearAuthenticationState();
            ClearToken();
            
            _logger.LogInformation("认证缓存已清除");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除认证缓存异常");
            return ServiceResult.Failure($"清除认证缓存异常: {ex.Message}");
        }
    }
    
    #endregion
}