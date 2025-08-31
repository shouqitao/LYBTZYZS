using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LoginRequest = LYBT.Shared.Models.Contracts.Auth.LoginRequest;
using LoginResponse = LYBT.Shared.Models.Contracts.Auth.LoginResponse;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 身份认证服务 - UltraThink精简版
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthApi _authApi;
        private readonly ITokenManager _tokenManager;
        private readonly ILogger<AuthenticationService>? _logger;
        private readonly SemaphoreSlim _authSemaphore = new(1, 1);
        private AuthenticationState _authState = new();

        public AuthenticationService(
            IAuthApi authApi,
            ITokenManager tokenManager,
            ILogger<AuthenticationService>? logger = null)
        {
            _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _logger = logger;
        }

        #region Properties

        public bool IsLoggedIn => _authState.IsAuthenticated;

        #endregion

        #region Core Authentication

        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            if (request == null)
                return ServiceResult<LoginResponse>.Failure("登录请求不能为空");

            await _authSemaphore.WaitAsync();
            try
            {
                _logger?.LogInformation("用户登录: {Username}", request.Username);

                var apiResponse = await _authApi.LoginAsync(request);
                if (!apiResponse.Success || apiResponse.Data == null)
                {
                    return ServiceResult<LoginResponse>.Failure(apiResponse.Message ?? "登录失败");
                }

                var response = new LoginResponse
                {
                    Token = apiResponse.Data.Token?.ToString() ?? string.Empty,
                    User = ConvertToUserDto(apiResponse.Data.User)
                };

                UpdateAuthenticationState(response);
                _logger?.LogInformation("登录成功: {UserId}", response.User?.Id);

                return ServiceResult<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "登录异常");
                return ServiceResult<LoginResponse>.Failure("登录失败: " + ex.Message);
            }
            finally
            {
                _authSemaphore.Release();
            }
        }

        public async Task<ServiceResult> LogoutAsync()
        {
            await _authSemaphore.WaitAsync();
            try
            {
                _logger?.LogInformation("用户登出");

                try
                {
                    await _authApi.LogoutAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "服务器登出失败，继续清除本地状态");
                }

                ClearAuthenticationState();
                return ServiceResult.Success();
            }
            finally
            {
                _authSemaphore.Release();
            }
        }

        #endregion

        #region User Info

        public Task<UserDto?> GetCurrentUserAsync()
        {
            return Task.FromResult(_authState.CurrentUser);
        }

        public Task<UserDto?> GetCurrentUserForUIAsync()
            => Task.FromResult(_authState.CurrentUser);

        public Task<ServiceResult<bool>> IsAuthenticatedAsync()
            => Task.FromResult(ServiceResult<bool>.Success(_authState.IsAuthenticated));

        public string? GetToken() => _tokenManager.GetToken();

        public void ClearAuthInfo() => ClearAuthenticationState();

        #endregion

        #region Token Management

        public Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return Task.FromResult(ServiceResult<bool>.Success(false));

            var currentToken = _tokenManager.GetToken();
            var isValid = token == currentToken && _authState.IsAuthenticated;
            return Task.FromResult(ServiceResult<bool>.Success(isValid));
        }

        public Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (!_authState.IsAuthenticated)
                return Task.FromResult(ServiceResult<LoginResponse>.Failure("用户未登录"));

            var response = new LoginResponse
            {
                Token = _tokenManager.GetToken() ?? string.Empty,
                User = _authState.CurrentUser ?? new UserDto()
            };

            return Task.FromResult(ServiceResult<LoginResponse>.Success(response));
        }

        #endregion

        #region Connection Check

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                
                var baseUrl = Core.Configuration.ApiConfiguration.BaseUrl.TrimEnd('/');
                var response = await httpClient.GetAsync($"{baseUrl}/swagger/index.html", cts.Token);
                
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Private Methods

        private UserDto ConvertToUserDto(dynamic userObj)
        {
            if (userObj == null) return new UserDto();

            try
            {
                return new UserDto
                {
                    Id = Guid.TryParse(userObj.Id?.ToString(), out Guid id) ? id : Guid.Empty,
                    Username = userObj.Username?.ToString() ?? string.Empty,
                    RealName = userObj.RealName?.ToString() ?? string.Empty,
                    PhoneNumber = userObj.PhoneNumber?.ToString(),
                    Email = userObj.Email?.ToString(),
                    Role = userObj.Role?.ToString() ?? "User",
                    Status = Enum.TryParse<LYBT.Shared.Models.Enums.CommonStatus>(userObj.Status?.ToString(), out LYBT.Shared.Models.Enums.CommonStatus status) 
                        ? status : LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    PinYinCode = userObj.PinYinCode?.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "用户信息转换失败");
                return new UserDto();
            }
        }

        private void UpdateAuthenticationState(LoginResponse response)
        {
            _authState = new AuthenticationState
            {
                IsAuthenticated = true,
                CurrentUser = response.User,
                Token = response.Token,
                AuthenticatedAt = DateTime.Now
            };

            _tokenManager.SetToken(response.Token);
        }

        private void ClearAuthenticationState()
        {
            _authState = new AuthenticationState();
            _tokenManager.ClearToken();
        }

        #endregion

        #region Internal Classes

        private class AuthenticationState
        {
            public bool IsAuthenticated { get; set; }
            public UserDto? CurrentUser { get; set; }
            public string? Token { get; set; }
            public DateTime? AuthenticatedAt { get; set; }
        }

        #endregion
    }
}