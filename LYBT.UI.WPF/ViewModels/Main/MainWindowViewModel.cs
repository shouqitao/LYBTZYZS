using System.Windows;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Events;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class MainWindowViewModel : BindableBase {
        private bool _isLoginVisible = true;
        public bool IsLoginVisible { get => _isLoginVisible; set => SetProperty(ref _isLoginVisible, value); }

        private bool _isMainVisible = false;
        public bool IsMainVisible { get => _isMainVisible; set => SetProperty(ref _isMainVisible, value); }

        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        public MainWindowViewModel(IEventAggregator eventAggregator, IRegionManager regionManager) {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);
        }

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
