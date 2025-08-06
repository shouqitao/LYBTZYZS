using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// API功能测试服务
    /// </summary>
    public class ApiTestService
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserService _userService;

        public ApiTestService(IAuthenticationService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        /// <summary>
        /// 执行完整的API功能测试
        /// </summary>
        public async Task<string> RunFullApiTestAsync()
        {
            var report = "=== LYBT WPF API 功能测试报告 ===\n\n";
            
            try
            {
                // 1. 测试登录功能
                report += "1. 测试登录功能...\n";
                var loginResult = await TestLoginAsync();
                report += loginResult + "\n\n";

                if (!_authService.IsLoggedIn)
                {
                    report += "❌ 登录失败，终止后续测试\n";
                    return report;
                }

                // 2. 测试用户管理功能
                report += "2. 测试用户管理功能...\n";
                var userTestResult = await TestUserManagementAsync();
                report += userTestResult + "\n\n";

                // 3. 测试Token验证
                report += "3. 测试Token验证...\n";
                var tokenTestResult = await TestTokenValidationAsync();
                report += tokenTestResult + "\n\n";

                report += "=== 测试完成 ===\n";
                report += $"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            }
            catch (Exception ex)
            {
                report += $"❌ 测试过程中发生异常: {ex.Message}\n";
            }

            return report;
        }

        /// <summary>
        /// 测试登录功能
        /// </summary>
        private async Task<string> TestLoginAsync()
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = "sysadmin",
                    Password = "Admin@123456",
                    RememberMe = true,
                    ClientIp = "192.168.190.243",
                    UserAgent = "LYBT.WPF.Client.Test",
                    LoginType = "Desktop"
                };

                var response = await _authService.LoginAsync(loginRequest);
                
                if (response.IsSuccess && response.Data != null)
                {
                    var user = response.Data.User;
                    var token = _authService.GetToken();
                    
                    return $"✅ 登录成功\n" +
                           $"   用户: {user.RealName} ({user.Username})\n" +
                           $"   角色: {user.Role}\n" +
                           $"   Token: {token?.Substring(0, 20)}...\n" +
                           $"   登录时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                }
                else
                {
                    return $"❌ 登录失败: {response.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                return $"❌ 登录测试异常: {ex.Message}";
            }
        }

        /// <summary>
        /// 测试用户管理功能
        /// </summary>
        private async Task<string> TestUserManagementAsync()
        {
            try
            {
                var result = "";

                // 测试分页查询用户
                var queryRequest = new UserPagedQueryDto
                {
                    CurrentPage = 1,
                    PageSize = 10,
                    SearchKeyword = null
                };

                var users = await _userService.SearchUsersAsync(queryRequest);
                result += $"✅ 用户分页查询成功\n";
                result += $"   总用户数: {users.TotalCount}\n";
                result += $"   当前页用户数: {users.Items.Count}\n";

                if (users.Items.Count > 0)
                {
                    result += "   用户列表:\n";
                    foreach (var user in users.Items)
                    {
                        result += $"     - {user.RealName} ({user.Username}) - {user.Role} - {(user.IsActive ? "启用" : "禁用")}\n";
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"❌ 用户管理测试异常: {ex.Message}";
            }
        }

        /// <summary>
        /// 测试Token验证
        /// </summary>
        private async Task<string> TestTokenValidationAsync()
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    return $"✅ Token验证成功\n" +
                           $"   当前用户: {currentUser.RealName}\n" +
                           $"   用户ID: {currentUser.Id}\n" +
                           $"   角色: {currentUser.Role}";
                }
                else
                {
                    return "❌ Token验证失败，无法获取当前用户信息";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Token验证测试异常: {ex.Message}";
            }
        }

        /// <summary>
        /// 显示测试结果对话框
        /// </summary>
        public static void ShowTestResult(string result)
        {
            var window = new Window
            {
                /* Title = "API功能测试结果", */
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = result,
                IsReadOnly = true,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(10),
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.LightGreen
            };

            window.Content = textBox;
            window.ShowDialog();
        }
    }
}