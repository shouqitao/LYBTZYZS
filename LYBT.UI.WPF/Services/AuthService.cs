using LYBT.Common.Enums.Users;
using LYBT.Module.Auth.Dtos;
using LYBT.UI.WPF.Apis;
using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 认证服务实现（带Token、记住密码、自动登录）
    /// </summary>
    public class AuthService : IAuthService {
        private readonly IAuthApi _authApi;
        private string _token;
        private bool _hasRemembered;
        private string _rememberedUserName;
        private string _rememberedPassword;

        public string Token => _token;
        public bool HasRemembered => _hasRemembered;
        public string RememberedUserName => _rememberedUserName;
        public string RememberedPassword => _rememberedPassword;

        private readonly string _autoLoginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autologin.json");

        public AuthService(IAuthApi authApi) {
            _authApi = authApi;
            if (File.Exists(_autoLoginPath)) {
                var json = File.ReadAllText(_autoLoginPath);
                var info = JsonSerializer.Deserialize<AutoLoginInfo>(json);
                if (info != null && !string.IsNullOrEmpty(info.UserName) && !string.IsNullOrEmpty(info.Password)) {
                    _hasRemembered = true;
                    _rememberedUserName = info.UserName;
                    _rememberedPassword = info.Password;
                }
            }
        }

        /// <summary>
        /// 方法 LoginAsync 的说明
        /// </summary>
        public async Task<(bool success, IList<UserRole> roles, string errorMessage, string token)> LoginAsync(string userName, string password) {
            try {
                var result = await _authApi.LoginAsync(new LoginRequestDto {
                    Username = userName,
                    Password = password
                });

                var roles = result.User?.Roles;
                if (roles == null || roles.Count == 0)
                    roles = new List<UserRole> { result.User.Role };

                _token = result.Token;
                SaveAutoLoginInfo(userName, password);

                return (true, roles, null, _token);
            } catch (ApiException) {
                return (false, null, "登录失败，用户名或密码错误！", null);
            } catch (Exception ex) {
                return (false, null, $"系统异常：{ex.Message}", null);
            }
        }

        /// <summary>
        /// 方法 SaveAutoLoginInfo 的说明
        /// </summary>
        private void SaveAutoLoginInfo(string userName, string password) {
            var info = new AutoLoginInfo { UserName = userName, Password = password };
            var json = JsonSerializer.Serialize(info);
            File.WriteAllText(_autoLoginPath, json);

            _hasRemembered = true;
            _rememberedUserName = userName;
            _rememberedPassword = password;
        }

        /// <summary>
        /// 方法 ClearAutoLoginInfo 的说明
        /// </summary>
        public void ClearAutoLoginInfo() {
            if (File.Exists(_autoLoginPath))
                File.Delete(_autoLoginPath);
            _hasRemembered = false;
            _rememberedUserName = null;
            _rememberedPassword = null;
        }

        /// <summary>
        /// 类 AutoLoginInfo 的说明
        /// </summary>
        private class AutoLoginInfo {
            /// <summary>
            /// 属性 UserName 的说明
            /// </summary>
            public string UserName { get; set; }
            /// <summary>
            /// 属性 Password 的说明
            /// </summary>
            public string Password { get; set; }
        }
    }
}
