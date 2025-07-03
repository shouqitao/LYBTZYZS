using LYBT.Common.Enums.Users;
using LYBT.Module.Auth.Services;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.Services;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 类 MainWindowViewModel 的说明
    /// </summary>
    public class MainWindowViewModel : BindableBase {
        private bool _isLoginVisible = true;
        /// <summary>
        /// 属性 IsLoginVisible 的说明
        /// </summary>
        public bool IsLoginVisible { get => _isLoginVisible; set => SetProperty(ref _isLoginVisible, value); }

        private bool _isMainVisible = false;
        /// <summary>
        /// 属性 IsMainVisible 的说明
        /// </summary>
        public bool IsMainVisible { get => _isMainVisible; set => SetProperty(ref _isMainVisible, value); }

        /// <summary>
        /// 属性 LogoutCommand 的说明
        /// </summary>
        public DelegateCommand LogoutCommand { get; }

        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        private readonly IAuthService _authService;

        public MainWindowViewModel(IEventAggregator eventAggregator, IRegionManager regionManager, IAuthService authService) {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
            _authService = authService;

            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 初始化退出命令
            LogoutCommand = new DelegateCommand(Logout);
        }

        /// <summary>
        /// 方法 Logout 的说明
        /// </summary>
        private void Logout() {
            // 显示登录界面，隐藏主界面
            IsMainVisible = false;
            IsLoginVisible = true;

            // 跳转回登录区
            _regionManager.RequestNavigate("LoginRegion", "LoginView");

            // 恢复窗口尺寸（可选）
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            Application.Current.MainWindow.Width = 420;
            Application.Current.MainWindow.Height = 480;

            // 清除自动登录信息
            _authService.ClearAutoLoginInfo();
        }

        /// <summary>
        /// 方法 OnLoginSuccess 的说明
        /// </summary>
        private void OnLoginSuccess(IList<UserRole> roles) {
            IsLoginVisible = false;
            IsMainVisible = true;
            // 最大化窗口
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            // 导航到主内容区（如HomeView）
            _regionManager.RequestNavigate("MainContentRegion", "HomeView", new NavigationParameters { { "UserRoles", roles } });
        }
    }
}
