using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Services;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 简化版身份认证服务 - UltraThink v2.0 精简架构
    /// 移除冗余功能，只保留核心认证逻辑
    /// </summary>
    public class SimplifiedAuthenticationService : IAuthenticationService
    {
        #region 私有字段

        private readonly IAuthApi _authApi;
        private readonly ITokenManager _tokenManager;
        private readonly ILogger<SimplifiedAuthenticationService>? _logger;
        
        private bool _isAuthenticated;
        private UserDto? _currentUser;
        
        #endregion

        #region 构造函数

        public SimplifiedAuthenticationService(
            IAuthApi authApi,
            ITokenManager tokenManager,
            ILogger<SimplifiedAuthenticationService>? logger = null)
        {
            _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _logger = logger;
        }

        #endregion

        #region 公共属性

        public bool IsLoggedIn => _isAuthenticated && _currentUser != null;

        #endregion

        #region 核心认证方法

        /// <summary>
        /// 用户登录 - 简化版本
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger?.LogInformation("开始登录: {Username}", request.Username);

                var response = await _authApi.LoginAsync(request);
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var loginResponse = response.Content;
                    
                    // 更新认证状态
                    _isAuthenticated = true;
                    _currentUser = loginResponse.User;
                    _tokenManager.SetToken(loginResponse.Token);

                    _logger?.LogInformation("登录成功: {UserId}", loginResponse.User?.Id);
                    return ServiceResult<LoginResponse>.Success(loginResponse);
                }

                var error = response.Error?.Content ?? "登录失败";
                return ServiceResult<LoginResponse>.Failure(error);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "登录异常");
                return ServiceResult<LoginResponse>.Failure($"登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 用户登出 - 简化版本
        /// </summary>
        public async Task<ServiceResult> LogoutAsync()
        {
            try
            {
                // 尝试调用服务器登出（失败不影响本地清理）
                try
                {
                    await _authApi.LogoutAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "服务器登出失败，继续本地清理");
                }

                // 清理本地状态
                ClearAuthInfo();
                
                _logger?.LogInformation("登出完成");
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "登出异常");
                return ServiceResult.Failure($"登出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前用户 - 简化版本
        /// </summary>
        public Task<UserDto?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        /// <summary>
        /// 获取Token
        /// </summary>
        public string? GetToken()
        {
            return _tokenManager.GetToken();
        }

        /// <summary>
        /// 清除认证信息
        /// </summary>
        public void ClearAuthInfo()
        {
            _isAuthenticated = false;
            _currentUser = null;
            _tokenManager.ClearToken();
        }

        /// <summary>
        /// 简单的连接检查
        /// </summary>
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var response = await _authApi.HealthCheckAsync();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}