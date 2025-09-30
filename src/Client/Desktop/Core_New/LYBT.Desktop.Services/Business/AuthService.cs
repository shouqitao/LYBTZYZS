using System;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 认证服务实现 - UltraThink架构
    /// TODO: 需要重构以实现Shared.Interfaces.IAuthService接口
    /// 当前接口签名与Shared.Interfaces.IAuthService不兼容，需要单独Issue处理
    /// </summary>
    public class AuthService
    {
        private readonly ILogger<AuthService> _logger;
        private UserDto _currentUser;

        public AuthService(ILogger<AuthService> logger)
        {
            _logger = logger;
        }

        public UserDto CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public Task<UserDto> LoginAsync(string username, string password)
        {
            if (username == "admin" && password == "admin")
            {
                _currentUser = new UserDto
                {
                    Id = Guid.NewGuid(),
                    UserName = username,
                    RealName = "管理员",
                    Role = UserRole.Admin,
                    Status = CommonStatus.Enabled
                };
                _logger.LogInformation($"用户登录成功: {username}");
                return Task.FromResult(_currentUser);
            }
            _logger.LogWarning($"用户登录失败: {username}");
            return Task.FromResult<UserDto>(null);
        }

        public Task<bool> LogoutAsync()
        {
            var username = _currentUser?.UserName;
            _currentUser = null;
            _logger.LogInformation($"用户登出: {username}");
            return Task.FromResult(true);
        }

        public Task<UserDto> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(IsAuthenticated);
        }

        public Task<bool> RefreshTokenAsync()
        {
            _logger.LogInformation("刷新Token");
            return Task.FromResult(IsAuthenticated);
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            return Task.FromResult(!string.IsNullOrEmpty(token));
        }

        public Task<bool> HasPermissionAsync(string permission)
        {
            if (!IsAuthenticated) return Task.FromResult(false);

            if (_currentUser.Role == UserRole.Admin)
                return Task.FromResult(true);

            return Task.FromResult(false);
        }

        public Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            _logger.LogInformation("更改当前用户密码");
            return Task.FromResult(true);
        }
    }
}