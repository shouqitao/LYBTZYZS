using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using LYBT.UI.PrismWpf.Models;
using LYBT.UI.PrismWpf.Services;

namespace LYBT.UI.PrismWpf.ViewModels
{
    /// <summary>
    /// 登录窗口ViewModel
    /// </summary>
    public class LoginWindowViewModel : BindableBase
    {
        private readonly IAuthService _authService;
        private readonly IDialogService _dialogService;

        private string _userName = "sysadmin";
        private string _password = string.Empty;
        private bool _rememberMe = true;
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private bool _hasError = false;

        public LoginWindowViewModel(IAuthService authService, IDialogService dialogService)
        {
            _authService = authService;
            _dialogService = dialogService;

            LoginCommand = new DelegateCommand(async () => await LoginAsync(), CanLogin)
                .ObservesProperty(() => UserName)
                .ObservesProperty(() => Password)
                .ObservesProperty(() => IsLoading);
        }

        #region Properties

        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "请输入用户名")]
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "请输入密码")]
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 记住登录状态
        /// </summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// 是否可以登录
        /// </summary>
        public bool CanLogin => !string.IsNullOrWhiteSpace(UserName) && 
                               !string.IsNullOrWhiteSpace(Password) && 
                               !IsLoading;

        #endregion

        #region Commands

        /// <summary>
        /// 登录命令
        /// </summary>
        public ICommand LoginCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 执行登录
        /// </summary>
        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var request = new LoginRequest
                {
                    UserName = UserName.Trim(),
                    Password = Password,
                    RememberMe = RememberMe
                };

                var response = await _authService.LoginAsync(request);

                if (response.Success)
                {
                    // 登录成功，关闭登录窗口，显示主界面
                    LoginSuccessful?.Invoke();
                }
                else
                {
                    ErrorMessage = response.Message ?? "登录失败，请检查用户名和密码";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"登录过程中发生错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 设置密码（从界面密码框设置）
        /// </summary>
        public void SetPassword(string password)
        {
            Password = password;
        }

        /// <summary>
        /// 清除错误消息
        /// </summary>
        public void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        #endregion

        #region Events

        /// <summary>
        /// 登录成功事件
        /// </summary>
        public event Action? LoginSuccessful;

        #endregion
    }
}