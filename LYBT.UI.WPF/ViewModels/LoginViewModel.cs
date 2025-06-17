using Prism.Commands;
using Prism.Mvvm;
using System;
using Refit;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LYBT.Module.Auth.Dtos;
using LYBT.UI.WPF.Services;

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

        private readonly IAuthApi _authApi;
        private readonly Services.TokenService _tokenService;

        public ICommand LoginCommand { get; }

        public LoginViewModel(IAuthApi authApi, Services.TokenService tokenService) {
            _authApi = authApi;
            _tokenService = tokenService;
            LoginCommand = new DelegateCommand(async () => await OnLoginAsync());
        }

        private async Task OnLoginAsync() {
            var dto = new LoginRequestDto { Username = Username, Password = Password };
            try {
                var response = await _authApi.LoginAsync(dto);
                _tokenService.SetToken(response.Token);
                LoginSucceeded?.Invoke();
            } catch (ApiException ex) {
                MessageBox.Show(ex.Content ?? "用户名或密码错误", "登录失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            } catch (System.Exception ex) {
                MessageBox.Show($"无法连接到服务器: {ex.Message}", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
