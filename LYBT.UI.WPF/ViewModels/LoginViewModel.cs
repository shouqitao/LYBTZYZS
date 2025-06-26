using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels {

    /// <summary>
    /// LoginView 对应的视图模型，处理登录逻辑
    /// </summary>
    public class LoginViewModel : BindableBase {
        private string _username;

        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password;

        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        // 登录命令，按钮绑定此命令以触发登录动作
        public DelegateCommand LoginCommand { get; private set; }

        private readonly IAuthService _authService;
        private readonly IRegionManager _regionManager;

        /// <summary>
        /// 构造函数，使用依赖注入获取 AuthService 和 RegionManager
        /// </summary>
        public LoginViewModel(IAuthService authService, IRegionManager regionManager) {
            _authService = authService;
            _regionManager = regionManager;
            // 初始化命令，指定执行方法和可执行判定（可执行判定此处简单为非空校验）
            LoginCommand = new DelegateCommand(ExecuteLogin, CanExecuteLogin)
                               .ObservesProperty(() => Username)
                               .ObservesProperty(() => Password);
        }

        /// <summary>
        /// 执行登录逻辑的具体方法
        /// </summary>
        private void ExecuteLogin() {
            // 调用认证服务进行验证（此处为模拟逻辑）
            UserRole? role = _authService.Login(Username, Password);
            if (role.HasValue) {
                // 登录成功，携带角色信息导航到主内容视图HomeView
                var parameters = new NavigationParameters();
                parameters.Add("UserRole", role.Value);
                _regionManager.RequestNavigate("ContentRegion", "HomeView", parameters);
                // 将主窗口切换为最大化显示
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            } else {
                // 登录失败，简单处理：可以在UI上提示错误（此处可通过消息或其他方式反馈）
                // 例如： MessageBox.Show("登录失败，用户名或密码不正确！");
            }
        }

        /// <summary>
        /// 登录命令是否可执行的判定逻辑
        /// </summary>
        private bool CanExecuteLogin() {
            // 当用户名和密码都不为空时，命令才可执行
            return !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        }
    }
}