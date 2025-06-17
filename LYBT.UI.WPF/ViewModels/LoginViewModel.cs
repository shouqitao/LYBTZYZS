using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LYBT.Module.Auth.Dtos;

namespace LYBT.UI.WPF.ViewModels {
    /// <summary>
    /// Login page view model
    /// </summary>
    public class LoginViewModel : BindableBase {
        private string _username = string.Empty;
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public event Action? LoginSucceeded;

        public ICommand LoginCommand { get; }

        public LoginViewModel() {
            LoginCommand = new DelegateCommand(async () => await OnLoginAsync());
        }

        private async Task OnLoginAsync() {
            using var http = new HttpClient { BaseAddress = new System.Uri("http://localhost:5297/") };
            var dto = new LoginRequestDto { Username = Username, Password = Password };
            try {
                var response = await http.PostAsJsonAsync("api/auth/login", dto);
                if (response.IsSuccessStatusCode) {
                    LoginSucceeded?.Invoke();
                }
                else {
                    MessageBox.Show("用户名或密码错误", "登录失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (HttpRequestException ex) {
                MessageBox.Show($"无法连接到服务器: {ex.Message}", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
