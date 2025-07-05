using LYBT.Common.Enums.Users;
using LYBT.Module.Auth.Dtos;
using LYBT.UI.WPF.Apis;
using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 认证服务实现，提供登录、登出、Token管理、自动登录等功能
    /// </summary>
    public class AuthService : IAuthService {
        private readonly IAuthApi _authApi;
        private string _token = string.Empty;
        private Guid _userId = Guid.Empty;
        private bool _hasRemembered;
        private string _rememberedUserName = string.Empty;
        private string _rememberedPassword = string.Empty;

        private readonly string _autoLoginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autologin.json");

        public string Token => _token;
        public Guid UserId => _userId;
        public bool HasRemembered => _hasRemembered;
        public string RememberedUserName => _rememberedUserName;
        public string RememberedPassword => _rememberedPassword;

        public AuthService(IAuthApi authApi) {
            _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
            LoadAutoLoginInfo();
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<(bool success, IList<UserRole> roles, string errorMessage, string token)> LoginAsync(string userName, string password) {
            try {
                // 验证输入参数
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password)) {
                    return (false, new List<UserRole>(), "用户名和密码不能为空", string.Empty);
                }

                // 调用 API 登录
                var loginRequest = new LoginRequestDto {
                    Username = userName,
                    Password = password,
                    LoginType = "Password"
                };

                var result = await _authApi.LoginAsync(loginRequest);

                // 验证返回结果
                if (result?.Data?.User == null) {
                    return (false, new List<UserRole>(), "登录失败：服务器返回数据异常", string.Empty);
                }

                // 获取用户角色
                var roles = GetUserRoles(result.Data.User);

                // 验证角色信息
                if (roles.Count == 0) {
                    return (false, new List<UserRole>(), "登录失败：用户未分配有效角色", string.Empty);
                }

                // 保存登录状态
                _token = result.Data.Token ?? string.Empty;
                TokenProvider.Token = _token;
                _userId = result.Data.User.Id;
                SaveAutoLoginInfo(userName, password);

                return (true, roles, string.Empty, _token);
            }
            catch (ApiException apiEx) {
                // API 异常处理
                var errorMessage = GetApiErrorMessage(apiEx);
                return (false, new List<UserRole>(), errorMessage, string.Empty);
            }
            catch (Exception ex) {
                // 其他异常处理
                return (false, new List<UserRole>(), $"登录异常：{ex.Message}", string.Empty);
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<bool> LogoutAsync() {
            try {
                if (!string.IsNullOrEmpty(_rememberedUserName)) {
                    var logoutRequest = new LogoutRequestDto { Username = _rememberedUserName };
                    var response = await _authApi.LogoutAsync(logoutRequest);

                    if (response?.Code == 200) {
                        ClearLoginState();
                        return true;
                    }
                }
                
                // 即使 API 调用失败，也清除本地状态
                ClearLoginState();
                return true;
            }
            catch {
                // 登出失败也清除本地状态
                ClearLoginState();
                return false;
            }
        }

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        public async Task<bool> ChangeSysAdminPasswordAsync(string oldPassword, string newPassword) {
            try {
                var resp = await _authApi.ChangeSysAdminPasswordAsync(new ChangeSysAdminPasswordDto {
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                });
                return resp?.Code == 200;
            } catch { return false; }
        }

        /// <summary>
        /// 清除自动登录信息
        /// </summary>
        public void ClearAutoLoginInfo() {
            try {
                if (File.Exists(_autoLoginPath)) {
                    File.Delete(_autoLoginPath);
                }
            }
            catch {
                // 忽略文件删除异常
            }

            _hasRemembered = false;
            _rememberedUserName = string.Empty;
            _rememberedPassword = string.Empty;
        }

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        private static IList<UserRole> GetUserRoles(Module.Users.Dtos.UserDto user) {
            var roles = new List<UserRole>();

            // 优先使用 Roles 列表
            if (user.Roles?.Any() == true) {
                roles.AddRange(user.Roles);
            }
            // 如果 Roles 为空，使用单个 Role
            else if (Enum.IsDefined(typeof(UserRole), user.Role)) {
                roles.Add(user.Role);
            }

            // 去重并排序
            return roles.Distinct().OrderBy(r => (int)r).ToList();
        }

        /// <summary>
        /// 获取 API 异常错误信息
        /// </summary>
        private static string GetApiErrorMessage(ApiException apiEx) {
            return apiEx.StatusCode switch {
                System.Net.HttpStatusCode.Unauthorized => "用户名或密码错误",
                System.Net.HttpStatusCode.Forbidden => "账户已被禁用",
                System.Net.HttpStatusCode.NotFound => "登录服务不可用",
                System.Net.HttpStatusCode.InternalServerError => "服务器内部错误",
                _ => $"登录失败：{apiEx.Content}"
            };
        }

        /// <summary>
        /// 加载自动登录信息
        /// </summary>
        private void LoadAutoLoginInfo() {
            try {
                if (!File.Exists(_autoLoginPath)) return;

                var json = File.ReadAllText(_autoLoginPath);
                var info = JsonSerializer.Deserialize<AutoLoginInfo>(json);

                if (info != null && !string.IsNullOrWhiteSpace(info.UserName) && !string.IsNullOrWhiteSpace(info.Password)) {
                    _hasRemembered = true;
                    _rememberedUserName = info.UserName;
                    _rememberedPassword = info.Password;
                }
            }
            catch {
                // 忽略加载异常，继续正常流程
            }
        }

        /// <summary>
        /// 保存自动登录信息
        /// </summary>
        private void SaveAutoLoginInfo(string userName, string password) {
            try {
                var info = new AutoLoginInfo { UserName = userName, Password = password };
                var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_autoLoginPath, json);

                _hasRemembered = true;
                _rememberedUserName = userName;
                _rememberedPassword = password;
            }
            catch {
                // 忽略保存异常
            }
        }

        /// <summary>
        /// 清除登录状态
        /// </summary>
        private void ClearLoginState() {
            _token = string.Empty;
            TokenProvider.Token = string.Empty;
            _userId = Guid.Empty;
            ClearAutoLoginInfo();
        }

        /// <summary>
        /// 自动登录信息存储类
        /// </summary>
        private class AutoLoginInfo {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
