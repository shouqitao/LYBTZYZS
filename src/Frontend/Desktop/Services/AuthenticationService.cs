using System;
using System.Threading.Tasks;
using System.Text.Json;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Enums;
using UserInfo = LYBT.WPF.Client.Core.Models.Users.UserInfo;

namespace LYBT.WPF.Client.Services {
    /// <summary>
    /// 真实的身份认证服务实现
    /// </summary>
    public class AuthenticationService : IAuthenticationService {
        private readonly IAuthApiService _authApiService;
        private readonly ITokenManager _tokenManager;
        private bool _isLoggedIn = false;
        private UserInfo? _currentUser;

        public AuthenticationService(IAuthApiService authApiService, ITokenManager tokenManager) {
            _authApiService = authApiService;
            _tokenManager = tokenManager;
        }

        public bool IsLoggedIn => _isLoggedIn;

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request) {
            try {
                // 创建后端API格式的登录请求
                var loginDto = new {
                    username = request.Username,
                    password = request.Password,
                    rememberMe = request.RememberMe,
                    clientIp = request.ClientIp,
                    userAgent = request.UserAgent,
                    loginType = request.LoginType
                };

                // 使用真实登录端点
                var response = await _authApiService.LoginAsync(loginDto);

                if (response.IsSuccess && response.Data != null) {
                    _isLoggedIn = true;
                    _tokenManager.SetToken(response.Data.Token);

                    // 将API返回的共享UserInfo转换为前端UserInfo模型
                    _currentUser = ConvertSharedUserInfoToFrontend(response.Data.User);
                }

                // 转换为前端类型
                return ConvertApiResponse(response);
            } catch (Exception ex) {
                return new ApiResponse<LoginResponse> {
                    IsSuccess = false,
                    Message = $"登录过程中发生错误: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<object>> LogoutAsync() {
            try {
                // 获取当前用户信息用于登出
                var currentUser = await GetCurrentUserAsync();
                var logoutDto = new {
                    username = currentUser?.Username ?? "unknown",
                    token = _tokenManager.GetToken()
                };

                var response = await _authApiService.LogoutAsync(logoutDto);

                // 无论API调用是否成功，都清除本地登录状态
                ClearAuthInfo();

                return response.IsSuccess ? ConvertApiResponse(response) : new ApiResponse<object> {
                    IsSuccess = true,
                    Message = "已清除本地登录状态"
                };
            } catch (Exception ex) {
                // 即使API调用失败，也清除本地状态
                ClearAuthInfo();
                return new ApiResponse<object> {
                    IsSuccess = true,
                    Message = $"登出完成，但API调用失败: {ex.Message}"
                };
            }
        }

        public async Task<UserInfo?> GetCurrentUserAsync() {
            if (!_isLoggedIn || _currentUser == null)
                return null;

            // 可以考虑从API刷新用户信息
            // var response = await _apiService.GetAsync<UserInfo>("users/current");
            // if (response.Success && response.Data != null)
            // {
            //     _currentUser = response.Data;
            // }

            return _currentUser;
        }

        public string? GetToken() {
            return _tokenManager.GetToken();
        }

        public void ClearAuthInfo() {
            _isLoggedIn = false;
            _tokenManager.ClearToken();
            _currentUser = null;
        }

        /// <summary>
        /// 将共享UserInfo转换为前端UserInfo模型
        /// </summary>
        private UserInfo? ConvertSharedUserInfoToFrontend(LYBT.Shared.Models.Auth.UserInfo? sharedUser) {
            if (sharedUser == null)
                return null;

            return new UserInfo {
                Id = sharedUser.Id,
                Username = sharedUser.Username,
                RealName = sharedUser.RealName,
                Role = ParseUserRole(sharedUser.Role),
                IsActive = sharedUser.IsActive,
                CreateTime = DateTime.Now, // 使用当前时间
                LastLoginTime = null, // 共享模型没有这个字段
                Email = sharedUser.Email,
                PhoneNumber = sharedUser.PhoneNumber,
                IsSuperAdmin = sharedUser.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true
            };
        }

        /// <summary>
        /// 将API返回的用户对象转换为前端UserInfo模型（旧方法保留兼容性）
        /// </summary>
        private UserInfo? ConvertToUserInfo(object userObj) {
            try {
                if (userObj == null)
                    return null;

                // 将对象序列化再反序列化进行类型转换
                var json = JsonSerializer.Serialize(userObj);
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // 先反序列化为匿名类型获取字段
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var userName = root.TryGetProperty("userName", out var userNameProp) ? userNameProp.GetString() ?? "" : "";
                
                return new UserInfo {
                    Id = root.TryGetProperty("id", out var idProp) ? idProp.GetGuid() : Guid.Empty,
                    Username = userName,
                    RealName = root.TryGetProperty("realName", out var realNameProp) ? realNameProp.GetString() ?? "" : "",
                    Role = root.TryGetProperty("role", out var roleProp) ? ParseUserRole(roleProp.GetString()) : UserRole.Staff,
                    IsActive = root.TryGetProperty("isActive", out var isActiveProp) && isActiveProp.GetBoolean(),
                    CreateTime = root.TryGetProperty("createdTime", out var createdTimeProp) ? createdTimeProp.GetDateTime() : DateTime.Now,
                    LastLoginTime = root.TryGetProperty("lastLoginTime", out var lastLoginTimeProp) ? lastLoginTimeProp.GetDateTime() : null,
                    Email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null,
                    PhoneNumber = root.TryGetProperty("phoneNumber", out var phoneProp) ? phoneProp.GetString() : null,
                    // 检查isSuperAdmin字段，如果没有则根据用户名判断
                    IsSuperAdmin = root.TryGetProperty("isSuperAdmin", out var isSuperAdminProp) && isSuperAdminProp.GetBoolean()
                                   || userName.Equals("sysadmin", StringComparison.OrdinalIgnoreCase)
                };
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// 解析用户角色字符串
        /// </summary>
        private UserRole ParseUserRole(string? roleString) {
            if (string.IsNullOrEmpty(roleString))
                return UserRole.Staff;

            // 添加调试信息
            System.Diagnostics.Debug.WriteLine($"ParseUserRole: 输入角色字符串 = '{roleString}'");

            // 尝试直接解析枚举
            if (Enum.TryParse<UserRole>(roleString, true, out var role))
            {
                System.Diagnostics.Debug.WriteLine($"ParseUserRole: 枚举解析成功 = {role}");
                return role;
            }

            // 兼容性处理 - 处理可能的不同命名格式
            var result = roleString.ToLower() switch {
                "staff" => UserRole.Staff,
                "diagnosingdoctor" => UserRole.DiagnosingDoctor,
                "cashierstaff" => UserRole.CashierStaff,
                "pharmacystaff" => UserRole.PharmacyStaff,
                "physiotherapystaff" => UserRole.PhysiotherapyStaff,
                "admin" => UserRole.Admin,
                _ => UserRole.Staff
            };
            
            System.Diagnostics.Debug.WriteLine($"ParseUserRole: 兼容性处理结果 = {result}");
            return result;
        }

        /// <summary>
        /// 转换API响应类型
        /// </summary>
        private ApiResponse<LoginResponse> ConvertApiResponse(LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse> apiResponse)
        {
            return new ApiResponse<LoginResponse>
            {
                IsSuccess = apiResponse.IsSuccess,
                Message = apiResponse.Message,
                StatusCode = apiResponse.StatusCode,
                Data = apiResponse.Data != null ? new LoginResponse
                {
                    Token = apiResponse.Data.Token,
                    User = ConvertToUserInfo(apiResponse.Data.User) ?? new UserInfo()
                } : null
            };
        }

        /// <summary>
        /// 转换API响应类型 - 通用对象版本
        /// </summary>
        private ApiResponse<object> ConvertApiResponse(LYBT.Shared.Models.Common.ApiResponse<object> apiResponse)
        {
            return new ApiResponse<object>
            {
                IsSuccess = apiResponse.IsSuccess,
                Message = apiResponse.Message,
                StatusCode = apiResponse.StatusCode,
                Data = apiResponse.Data
            };
        }
    }
}