using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 登录界面视图模型，处理用户登录逻辑
    /// </summary>
    public class LoginViewModel : BindableBase {
        private readonly IAuthService _authService;
        private readonly IEventAggregator _eventAggregator;

        #region Properties

        private string _userName = string.Empty;
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _password = string.Empty;
        /// <summary>
        /// 密码
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isRemember;
        /// <summary>
        /// 是否记住密码
        /// </summary>
        public bool IsRemember {
            get => _isRemember;
            set => SetProperty(ref _isRemember, value);
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在登录
        /// </summary>
        public bool IsLoading {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _loadingText = "登录中...";
        /// <summary>
        /// 加载提示文本
        /// </summary>
        public string LoadingText {
            get => _loadingText;
            set => SetProperty(ref _loadingText, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 登录命令
        /// </summary>
        public DelegateCommand LoginCommand { get; }

        /// <summary>
        /// 清空表单命令
        /// </summary>
        public DelegateCommand ClearCommand { get; }

        #endregion

        public LoginViewModel(IAuthService authService, IEventAggregator eventAggregator) {
            _authService = authService ?? throw new System.ArgumentNullException(nameof(authService));
            _eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));

            // 初始化命令
            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin)
                .ObservesProperty(() => UserName)
                .ObservesProperty(() => Password)
                .ObservesProperty(() => IsLoading);

            ClearCommand = new DelegateCommand(ExecuteClear, () => !IsLoading)
                .ObservesProperty(() => IsLoading);

            // 加载记住的登录信息
            LoadRememberedCredentials();

            System.Diagnostics.Debug.WriteLine("LoginViewModel constructed");
        }

        #region Command Methods

        /// <summary>
        /// 执行登录操作
        /// </summary>
        private async Task ExecuteLoginAsync() {
            try {
                // 清除之前的错误信息
                ErrorMessage = string.Empty;
                IsLoading = true;
                LoadingText = "正在验证用户信息...";

                // 验证输入
                if (!ValidateInput()) {
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"开始登录用户: {UserName}");

                // 执行登录
                var (success, roles, errorMessage, token) = await _authService.LoginAsync(UserName.Trim(), Password);

                System.Diagnostics.Debug.WriteLine($"登录结果: success={success}, roles count={roles?.Count ?? 0}");

                if (success && roles?.Any() == true) {
                    // 登录成功
                    LoadingText = "登录成功，正在跳转...";

                    System.Diagnostics.Debug.WriteLine($"发布登录成功事件，角色: {string.Join(", ", roles)}");

                    // 保存记住的登录信息（如果选择了记住密码）
                    if (IsRemember) {
                        // 这里可以调用 _authService 的保存记住信息的方法
                        // _authService.SaveRememberedCredentials(UserName, Password);
                    } else {
                        _authService.ClearAutoLoginInfo();
                    }

                    // 发布登录成功事件
                    _eventAggregator.GetEvent<LoginSuccessEvent>().Publish(roles);

                    // 清除密码（如果不记住的话）
                    if (!IsRemember) {
                        Password = string.Empty;
                    }
                }
                else {
                    // 登录失败
                    ErrorMessage = errorMessage ?? "登录失败，请检查用户名和密码";
                    System.Diagnostics.Debug.WriteLine($"登录失败: {ErrorMessage}");

                    // 清除密码
                    Password = string.Empty;
                }
            }
            catch (System.Exception ex) {
                ErrorMessage = $"登录异常：{ex.Message}";
                System.Diagnostics.Debug.WriteLine($"登录异常: {ex}");
            }
            finally {
                IsLoading = false;
                LoadingText = "登录中...";
            }
        }

        /// <summary>
        /// 判断是否可以执行登录
        /// </summary>
        private bool CanExecuteLogin() {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(UserName) && 
                   !string.IsNullOrWhiteSpace(Password);
        }

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ExecuteClear() {
            UserName = string.Empty;
            Password = string.Empty;
            ErrorMessage = string.Empty;
            IsRemember = false;

            // 清除记住的登录信息
            _authService.ClearAutoLoginInfo();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 验证输入参数
        /// </summary>
        private bool ValidateInput() {
            if (string.IsNullOrWhiteSpace(UserName)) {
                ErrorMessage = "请输入用户名";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password)) {
                ErrorMessage = "请输入密码";
                return false;
            }

            if (UserName.Trim().Length < 2) {
                ErrorMessage = "用户名长度不能少于2个字符";
                return false;
            }

            if (Password.Length < 1) {
                ErrorMessage = "密码不能为空";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载记住的登录凭据
        /// </summary>
        private void LoadRememberedCredentials() {
            if (_authService.HasRemembered) {
                UserName = _authService.RememberedUserName ?? string.Empty;
                Password = _authService.RememberedPassword ?? string.Empty;
                IsRemember = true;
                System.Diagnostics.Debug.WriteLine("已加载记住的登录凭据");
            }
        }

        #endregion
    }
}
