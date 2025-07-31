using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 临时的身份认证服务实现，用于演示
    /// </summary>
    public class MockAuthenticationService : IAuthenticationService
    {
        private bool _isLoggedIn = false;
        private string _token = string.Empty;

        public bool IsLoggedIn => _isLoggedIn;

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            // 模拟登录延迟
            await Task.Delay(1500);

            // 简单的用户名密码验证
            if (request.Username == "sysadmin" && request.Password == "123456")
            {
                _isLoggedIn = true;
                _token = Guid.NewGuid().ToString();

                return new ApiResponse<LoginResponse>
                {
                    IsSuccess = true,
                    Message = "登录成功",
                    Data = new LoginResponse
                    {
                        Token = _token,
                        User = new LYBT.WPF.Client.Core.Models.Users.UserInfo
                        {
                            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            UserName = "sysadmin",
                            RealName = "系统管理员",
                            Role = LYBT.WPF.Client.Core.Enums.UserRole.SuperAdmin,
                            IsActive = true,
                            CreatedTime = DateTime.Now.AddDays(-30)
                        }
                    }
                };
            }

            return new ApiResponse<LoginResponse>
            {
                IsSuccess = false,
                Message = "用户名或密码错误"
            };
        }

        public async Task<ApiResponse<object>> LogoutAsync()
        {
            _isLoggedIn = false;
            _token = string.Empty;
            
            await Task.Delay(500);
            
            return new ApiResponse<object>
            {
                IsSuccess = true,
                Message = "注销成功"
            };
        }

        public async Task<LYBT.WPF.Client.Core.Models.Users.UserInfo?> GetCurrentUserAsync()
        {
            if (!_isLoggedIn)
                return null;

            await Task.Delay(200);

            return new LYBT.WPF.Client.Core.Models.Users.UserInfo
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserName = "sysadmin",
                RealName = "系统管理员",
                Role = LYBT.WPF.Client.Core.Enums.UserRole.SuperAdmin,
                IsActive = true,
                CreatedTime = DateTime.Now.AddDays(-30)
            };
        }

        public string? GetToken()
        {
            return _token;
        }

        public void ClearAuthInfo()
        {
            _isLoggedIn = false;
            _token = string.Empty;
        }
    }
}