using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using LYBT.WPF.Client.Core.Services;
using LYBT.Shared.Models.Auth;
using LYBT.WPF.Client.Core.Models;
using Refit;
using System.Net;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Enums;
using UserInfo = LYBT.WPF.Client.Core.Models.Users.UserInfo;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 真实的身份认证服务实现
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthApiService _authApiService;
        private readonly ITokenManager _tokenManager;
        private bool _isLoggedIn = false;
        private UserInfo? _currentUser;

        public AuthenticationService(IAuthApiService authApiService, ITokenManager tokenManager)
        {
            _authApiService = authApiService;
            _tokenManager = tokenManager;
        }

        public bool IsLoggedIn => _isLoggedIn;

        public async Task<ServiceResult<LYBT.Shared.Models.Auth.LoginResponse>> LoginAsync(LoginRequest request)
        {
            // 转换 LoginRequest 类型
            var clientLoginRequest = new LYBT.WPF.Client.Core.Models.Authentication.LoginRequest
            {
                Username = request.Username,
                Password = request.Password,
                RememberMe = request.RememberMe
            };

            var result = await ApiErrorHandler.HandleApiResponseAsync(
                async () => await _authApiService.LoginAsync(clientLoginRequest)
            );

            // 处理双重包装：Refit.ApiResponse<ApiResponse<LoginResponseDto>>
            if (result.IsSuccess && result.Data != null && result.Data.Success && result.Data.Data != null && !string.IsNullOrEmpty(result.Data.Data.Token))
            {
                _isLoggedIn = true;
                _tokenManager.SetToken(result.Data.Data.Token);

                // 将BaseUserModel转换为前端UserInfo模型
                _currentUser = ConvertBaseUserModelToFrontend(result.Data.Data.User);

                // 转换DTO为API契约类型
                var authResponse = new LYBT.Shared.Models.Auth.LoginResponse
                {
                    Token = result.Data.Data.Token,
                    User = ConvertBaseUserModelToAuthUserInfo(result.Data.Data.User)
                };

                return ServiceResult<LYBT.Shared.Models.Auth.LoginResponse>.Success(authResponse);
            }

            // 从内层ApiResponse获取错误信息
            var errorMessage = result.Data?.Message ?? result.ErrorMessage ?? "登录失败";
            return ServiceResult<LYBT.Shared.Models.Auth.LoginResponse>.Failure(errorMessage, result.Exception);
        }

        public async Task<ServiceResult> LogoutAsync()
        {
            var result = await ApiErrorHandler.HandleApiCallAsync(
                async () => await _authApiService.LogoutAsync()
            );

            // 无论API调用是否成功，都清除本地登录状态
            ClearAuthInfo();

            // 总是返回成功，因为本地状态已清除
            return ServiceResult.Success();
        }

        public Task<LYBT.WPF.Client.Core.Models.Users.UserInfo?> GetCurrentUserAsync()
        {
            if (!_isLoggedIn || _currentUser == null)
                return Task.FromResult<LYBT.WPF.Client.Core.Models.Users.UserInfo?>(null);

            // 可以考虑从API刷新用户信息
            // var response = await _apiService.GetAsync<BaseUserModel>("users/current");
            // if (response.Success && response.Data != null)
            // {
            //     _currentUser = response.Data;
            // }

            return Task.FromResult<LYBT.WPF.Client.Core.Models.Users.UserInfo?>(_currentUser);
        }

        public string? GetToken()
        {
            return _tokenManager.GetToken();
        }

        public void ClearAuthInfo()
        {
            _isLoggedIn = false;
            _tokenManager.ClearToken();
            _currentUser = null;
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                // 忽略SSL证书错误（仅用于开发环境）
                var handler = new System.Net.Http.HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new System.Net.Http.HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(3);

                // 从配置获取API基础URL，使用swagger作为健康检查端点
                var baseUrl = LYBT.WPF.Client.Core.Configuration.ApiConfiguration.BaseUrl.TrimEnd('/');
                var response = await client.GetAsync($"{baseUrl}/swagger/index.html");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                // 如果发生任何异常，认为API不可用
                return false;
            }
        }

        /// <summary>
        /// 将BaseUserModel转换为前端UserInfo模型
        /// </summary>
        private UserInfo? ConvertBaseUserModelToFrontend(LYBT.Shared.Models.Core.BaseUserModel? baseUser)
        {
            if (baseUser == null)
                return null;

            return new UserInfo
            {
                Id = baseUser.Id,
                Username = baseUser.Username,
                RealName = baseUser.RealName,
                Status = baseUser.Status,
                CreateTime = baseUser.CreateTime,
                LastLoginTime = baseUser.LastLoginTime,
                PhoneNumber = baseUser.PhoneNumber,
            };
        }

        /// <summary>
        /// 将BaseUserModel转换为Auth.UserInfo模型
        /// </summary>
        private LYBT.Shared.Models.Auth.UserInfo ConvertBaseUserModelToAuthUserInfo(LYBT.Shared.Models.Core.BaseUserModel? baseUser)
        {
            if (baseUser == null)
                return new LYBT.Shared.Models.Auth.UserInfo();

            return new LYBT.Shared.Models.Auth.UserInfo
            {
                Id = baseUser.Id,
                Username = baseUser.Username,
                RealName = baseUser.RealName,
                PhoneNumber = baseUser.PhoneNumber
            };
        }

        /// <summary>
        /// 将API返回的用户对象转换为前端UserInfo模型（旧方法保留兼容性）
        /// </summary>
        private UserInfo? ConvertToUserInfo(object userObj)
        {
            try
            {
                if (userObj == null)
                    return null;

                // 将对象序列化再反序列化进行类型转换
                var json = JsonSerializer.Serialize(userObj);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // 先反序列化为匿名类型获取字段
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var userName = root.TryGetProperty("userName", out var userNameProp) ? userNameProp.GetString() ?? "" : "";

                return new UserInfo
                {
                    Id = root.TryGetProperty("id", out var idProp) ? idProp.GetGuid() : Guid.Empty,
                    Username = userName,
                    RealName = root.TryGetProperty("realName", out var realNameProp) ? realNameProp.GetString() ?? "" : "",
                    Status = root.TryGetProperty("status", out var statusProp) && statusProp.TryGetInt32(out var statusValue)
                        ? (CommonStatus)statusValue
                        : CommonStatus.Enabled,
                    CreateTime = root.TryGetProperty("createdTime", out var createdTimeProp) ? createdTimeProp.GetDateTime() : DateTime.Now,
                    LastLoginTime = root.TryGetProperty("lastLoginTime", out var lastLoginTimeProp) ? lastLoginTimeProp.GetDateTime() : null,
                    PhoneNumber = root.TryGetProperty("phoneNumber", out var phoneProp) ? phoneProp.GetString() : null
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 解析用户角色字符串
        /// </summary>
        private string ParseUserRole(string? roleString)
        {
            return "";
        }


    }
}