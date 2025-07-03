using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.Events;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 登录界面VM，登录成功发布事件
    /// </summary>
    public class LoginViewModel : BindableBase {
        private string _userName;
        /// <summary>
        /// 属性 UserName 的说明
        /// </summary>
        public string UserName { get => _userName; set => SetProperty(ref _userName, value); }

        private string _password;
        /// <summary>
        /// 属性 Password 的说明
        /// </summary>
        public string Password { get => _password; set => SetProperty(ref _password, value); }

        private string _errorMessage;
        /// <summary>
        /// 属性 ErrorMessage 的说明
        /// </summary>
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        private bool _isRemember;
        /// <summary>
        /// 属性 IsRemember 的说明
        /// </summary>
        public bool IsRemember { get => _isRemember; set => SetProperty(ref _isRemember, value); }

        /// <summary>
        /// 属性 LoginCommand 的说明
        /// </summary>
        public DelegateCommand LoginCommand { get; }

        private readonly IAuthService _authService;
        private readonly IEventAggregator _eventAggregator;

        public LoginViewModel(IAuthService authService, IEventAggregator eventAggregator) {
            _authService = authService;
            _eventAggregator = eventAggregator;

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin)
                .ObservesProperty(() => UserName)
                .ObservesProperty(() => Password);

            // 自动填充记住的账号
            if (_authService.HasRemembered) {
                UserName = _authService.RememberedUserName;
                Password = _authService.RememberedPassword;
                IsRemember = true;
            }
        }

        /// <summary>
        /// 方法 ExecuteLoginAsync 的说明
        /// </summary>
        private async Task ExecuteLoginAsync() {
            ErrorMessage = string.Empty;
            var (success, roles, errorMsg, token) = await _authService.LoginAsync(UserName, Password);
            if (success && roles != null && roles.Count > 0) {
                // 发布登录成功事件（主窗口收到后切换区域）
                _eventAggregator.GetEvent<LoginSuccessEvent>().Publish(roles);

                // 推荐此处再做一次主内容区导航（如果需要）
                // var regionManager = ...注入后 regionManager.RequestNavigate("ContentRegion", "HomeView");
            } else {
                ErrorMessage = errorMsg ?? "登录失败，用户名或密码不正确！";
            }
        }

        /// <summary>
        /// 方法 CanExecuteLogin 的说明
        /// </summary>
        private bool CanExecuteLogin() => !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password);
    }
}
