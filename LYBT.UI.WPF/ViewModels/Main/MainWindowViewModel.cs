using LYBT.Common.Enums.Users;
using LYBT.Module.Auth.Services;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using System.Windows;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 类 MainWindowViewModel 的说明
    /// </summary>
    public class MainWindowViewModel : BindableBase {
        private bool _isFunctionVisible = true;
        /// <summary>
        /// 功能区可见性（登录/修改密码等）
        /// </summary>
        public bool IsFunctionVisible { get => _isFunctionVisible; set => SetProperty(ref _isFunctionVisible, value); }

        private bool _isMainVisible = false;
        /// <summary>
        /// 属性 IsMainVisible 的说明
        /// </summary>
        public bool IsMainVisible { get => _isMainVisible; set => SetProperty(ref _isMainVisible, value); }

        private bool _isDoctorRole = false;
        /// <summary>
        /// 当前登录用户是否医生角色
        /// </summary>
        public bool IsDoctorRole { get => _isDoctorRole; set => SetProperty(ref _isDoctorRole, value); }

        private bool _hasDoctorProfile;
        public bool HasDoctorProfile { get => _hasDoctorProfile; set => SetProperty(ref _hasDoctorProfile, value); }

        private string _doctorProfileButtonText = "新增医生档案";
        public string DoctorProfileButtonText { get => _doctorProfileButtonText; set => SetProperty(ref _doctorProfileButtonText, value); }

        /// <summary>
        /// 退出登录命令
        /// </summary>
        public DelegateCommand LogoutCommand { get; }

        /// <summary>
        /// 显示修改密码界面
        /// </summary>
        public DelegateCommand ShowChangePasswordCommand { get; }

        /// <summary>
        /// 显示修改个人信息界面
        /// </summary>
        public DelegateCommand ShowChangeProfileCommand { get; }
        public DelegateCommand ShowDoctorProfileCommand { get; }


        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        private readonly IAuthService _authService;
        private readonly IDoctorService _doctorService;

        public MainWindowViewModel(IEventAggregator eventAggregator, IRegionManager regionManager, IAuthService authService, IDoctorService doctorService) {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
            _authService = authService;
            _doctorService = doctorService;

            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 初始化命令
            LogoutCommand = new DelegateCommand(Logout);
            ShowChangePasswordCommand = new DelegateCommand(ShowChangePassword);
            ShowChangeProfileCommand = new DelegateCommand(ShowChangeProfile);
            ShowDoctorProfileCommand = new DelegateCommand(ShowDoctorProfile);
        }

        /// <summary>
        /// 方法 Logout 的说明
        /// </summary>
        private void Logout() {
            // 显示登录界面，隐藏主界面
            IsMainVisible = false;
            IsFunctionVisible = true;

            // 跳转回登录区
            _regionManager.RequestNavigate("FunctionRegion", "LoginView");

            // 恢复窗口尺寸（可选）
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            Application.Current.MainWindow.Width = 420;
            Application.Current.MainWindow.Height = 480;

            // 清除自动登录信息
            _authService.ClearAutoLoginInfo();
        }

        private void ShowChangePassword() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            _regionManager.RequestNavigate("FunctionRegion", "ChangePasswordView");
        }

        private void ShowChangeProfile() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            _regionManager.RequestNavigate("FunctionRegion", "ChangeProfileView");
        }

        private void ShowDoctorProfile() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            _regionManager.RequestNavigate("FunctionRegion", "DoctorProfileView");
        }


        /// <summary>
        /// 方法 OnLoginSuccess 的说明
        /// </summary>
        private async void OnLoginSuccess(IList<UserRole> roles) {
            IsFunctionVisible = false;
            IsMainVisible = true;
            IsDoctorRole = roles.Contains(UserRole.DiagnosingDoctor) || roles.Contains(UserRole.TreatmentDoctor);
            // 新增：如果是管理员，直接跳转到系统管理界面
            if (roles.Contains(UserRole.Admin)) {
                // 这里假设有一个 UserManagementView 作为系统管理主界面
                _regionManager.RequestNavigate("MainContentRegion", "UserManagementView", new NavigationParameters { { "UserRoles", roles } });
                return;
            }
            if (IsDoctorRole)
                await CheckDoctorProfileAsync();
            // 最大化窗口
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            // 导航到主内容区（如HomeView）
            _regionManager.RequestNavigate("MainContentRegion", "HomeView", new NavigationParameters { { "UserRoles", roles } });
        }

        public async Task CheckDoctorProfileAsync() {
            try {
                var detail = await _doctorService.GetByUserIdAsync(_authService.UserId);
                HasDoctorProfile = detail != null;
                DoctorProfileButtonText = HasDoctorProfile ? "编辑医生档案" : "新增医生档案";
            } catch (Exception ex) {
                MessageBox.Show($"医生档案检查失败：{ex.Message}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
