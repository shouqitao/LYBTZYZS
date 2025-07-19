using System.Windows;
using System.Windows.Threading;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.Common.Enums;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 重构后的主窗口视图模型 - 增强功能和用户体验
    /// </summary>
    public class MainWindowViewModel : BindableBase {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IAuthService _authService;
        private readonly IDoctorService _doctorService;
        private readonly IThemeService _themeService;
        private readonly DispatcherTimer _statusTimer;

        #region Properties

        private bool _isFunctionVisible = true;
        /// <summary>
        /// 功能区可见性（登录/修改密码等）
        /// </summary>
        public bool IsFunctionVisible {
            get => _isFunctionVisible;
            set => SetProperty(ref _isFunctionVisible, value);
        }

        private bool _isMainVisible = false;
        /// <summary>
        /// 主界面可见性
        /// </summary>
        public bool IsMainVisible {
            get => _isMainVisible;
            set => SetProperty(ref _isMainVisible, value);
        }

        private bool _isDoctorRole = false;
        /// <summary>
        /// 当前登录用户是否医生角色
        /// </summary>
        public bool IsDoctorRole {
            get => _isDoctorRole;
            set => SetProperty(ref _isDoctorRole, value);
        }

        private bool _isSysAdmin;
        /// <summary>
        /// 当前登录用户是否为内置 sysadmin
        /// </summary>
        public bool IsSysAdmin {
            get => _isSysAdmin;
            set {
                if (SetProperty(ref _isSysAdmin, value)) {
                    RaisePropertyChanged(nameof(IsNotSysAdmin));
                }
            }
        }

        /// <summary>
        /// 非 sysadmin 用户
        /// </summary>
        public bool IsNotSysAdmin => !IsSysAdmin;

        private bool _hasDoctorProfile;
        /// <summary>
        /// 是否有医生档案
        /// </summary>
        public bool HasDoctorProfile {
            get => _hasDoctorProfile;
            set => SetProperty(ref _hasDoctorProfile, value);
        }

        private string _doctorProfileButtonText = "新增医生档案";
        /// <summary>
        /// 医生档案按钮文本
        /// </summary>
        public string DoctorProfileButtonText {
            get => _doctorProfileButtonText;
            set => SetProperty(ref _doctorProfileButtonText, value);
        }

        private bool _isNavDrawerOpen;
        /// <summary>
        /// 左侧导航抽屉是否展开
        /// </summary>
        public bool IsNavDrawerOpen {
            get => _isNavDrawerOpen;
            set => SetProperty(ref _isNavDrawerOpen, value);
        }

        private string _currentUserName = "未登录";
        /// <summary>
        /// 当前用户名
        /// </summary>
        public string CurrentUserName {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        private string _currentUserRole = "";
        /// <summary>
        /// 当前用户角色
        /// </summary>
        public string CurrentUserRole {
            get => _currentUserRole;
            set => SetProperty(ref _currentUserRole, value);
        }

        private string _systemStatus = "系统正常";
        /// <summary>
        /// 系统状态
        /// </summary>
        public string SystemStatus {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>
        /// 当前时间
        /// </summary>
        public string CurrentTime {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        private bool _isDarkTheme = false;
        /// <summary>
        /// 是否为暗色主题
        /// </summary>
        public bool IsDarkTheme {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        private IList<UserRole> _currentUserRoles = new List<UserRole>();
        /// <summary>
        /// 当前用户角色列表
        /// </summary>
        public IList<UserRole> CurrentUserRoles {
            get => _currentUserRoles;
            set => SetProperty(ref _currentUserRoles, value);
        }

        private bool _isInitialized = false;
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 退出登录命令
        /// </summary>
        public DelegateCommand LogoutCommand { get; private set; }

        /// <summary>
        /// 显示修改密码界面
        /// </summary>
        public DelegateCommand ShowChangePasswordCommand { get; private set; }

        /// <summary>
        /// 显示修改个人信息界面
        /// </summary>
        public DelegateCommand ShowChangeProfileCommand { get; private set; }

        /// <summary>
        /// 显示医生档案界面
        /// </summary>
        public DelegateCommand ShowDoctorProfileCommand { get; private set; }

        /// <summary>
        /// 显示患者档案界面
        /// </summary>
        public DelegateCommand ShowPatientProfileCommand { get; private set; }

        /// <summary>
        /// 切换主题命令
        /// </summary>
        public DelegateCommand ToggleThemeCommand { get; private set; }

        /// <summary>
        /// 切换导航抽屉
        /// </summary>
        public DelegateCommand ToggleNavDrawerCommand { get; private set; }

        /// <summary>
        /// 刷新状态命令
        /// </summary>
        public DelegateCommand RefreshStatusCommand { get; private set; }

        /// <summary>
        /// 关于系统命令
        /// </summary>
        public DelegateCommand AboutSystemCommand { get; private set; }

        /// <summary>
        /// 系统设置命令
        /// </summary>
        public DelegateCommand SystemSettingsCommand { get; private set; }

        #endregion

        public MainWindowViewModel(IEventAggregator eventAggregator, IRegionManager regionManager,
                                   IAuthService authService, IDoctorService doctorService,
                                   IThemeService themeService) {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 初始化命令
            InitializeCommands();

            // 初始化状态定时器
            InitializeStatusTimer();

            // 初始化主题状态
            IsDarkTheme = _themeService.IsDarkTheme;

            System.Diagnostics.Debug.WriteLine("Enhanced MainWindowViewModel constructed");
        }

        #region Initialization

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands() {
            LogoutCommand = new DelegateCommand(async () => await LogoutAsync());
            ShowChangePasswordCommand = new DelegateCommand(ShowChangePassword);
            ShowChangeProfileCommand = new DelegateCommand(ShowChangeProfile);
            ShowDoctorProfileCommand = new DelegateCommand(ShowDoctorProfile);
            ShowPatientProfileCommand = new DelegateCommand(ShowPatientProfile);
            ToggleThemeCommand = new DelegateCommand(ToggleTheme);
            ToggleNavDrawerCommand = new DelegateCommand(() => IsNavDrawerOpen = !IsNavDrawerOpen);
            RefreshStatusCommand = new DelegateCommand(async () => await RefreshSystemStatusAsync());
            AboutSystemCommand = new DelegateCommand(ShowAboutSystem);
            SystemSettingsCommand = new DelegateCommand(ShowSystemSettings);
        }

        /// <summary>
        /// 初始化状态定时器
        /// </summary>
        private void InitializeStatusTimer() {
            _statusTimer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusTimer.Tick += StatusTimer_Tick;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 状态定时器事件
        /// </summary>
        private void StatusTimer_Tick(object sender, EventArgs e) {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 登录成功事件处理
        /// </summary>
        private async void OnLoginSuccess(IList<UserRole> roles) {
            try {
                System.Diagnostics.Debug.WriteLine($"OnLoginSuccess called with roles: {string.Join(", ", roles)}");

                // 保存用户角色信息
                CurrentUserRoles = roles;

                // 更新用户信息
                await UpdateUserInfoAsync(roles);

                // 更新界面状态
                UpdateUIState();

                // 检查医生档案（如果是医生角色）
                if (IsDoctorRole) {
                    await CheckDoctorProfileAsync();
                }

                // 启动状态定时器
                _statusTimer.Start();

                // 最大化窗口
                MaximizeWindow();

                // 导航到主内容区
                await NavigateToHomeAsync(roles);

                // 刷新系统状态
                await RefreshSystemStatusAsync();

                // 标记为已初始化
                IsInitialized = true;

                System.Diagnostics.Debug.WriteLine("Main window initialization completed successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnLoginSuccess error: {ex.Message}");
                MessageBox.Show($"初始化主界面时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// 退出登录
        /// </summary>
        private async Task LogoutAsync() {
            try {
                var result = MessageBox.Show(
                    "确定要退出登录吗？",
                    "退出确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // 停止定时器
                _statusTimer?.Stop();

                // 执行登出
                await _authService.LogoutAsync();

                // 重置界面状态
                ResetUIState();

                // 跳转回登录界面
                NavigateToLogin();

                System.Diagnostics.Debug.WriteLine("User logged out successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
                MessageBox.Show($"退出登录时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示修改密码界面
        /// </summary>
        private void ShowChangePassword() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            _regionManager.RequestNavigate("FunctionRegion", "ChangePasswordView");
        }

        /// <summary>
        /// 显示修改个人信息界面
        /// </summary>
        private void ShowChangeProfile() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            _regionManager.RequestNavigate("FunctionRegion", "ChangeProfileView");
        }

        /// <summary>
        /// 显示医生档案界面
        /// </summary>
        private void ShowDoctorProfile() {
            NavigateDoctorProfile();
        }

        /// <summary>
        /// 显示患者档案界面
        /// </summary>
        private void ShowPatientProfile() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            _regionManager.RequestNavigate("FunctionRegion", "PatientProfileView", result => {
                var view = _regionManager.Regions["FunctionRegion"].ActiveViews.FirstOrDefault();
                if (view is FrameworkElement element && element.DataContext is PatientProfileViewModel vm) {
                    vm.CancelAction = () => {
                        IsMainVisible = true;
                        IsFunctionVisible = false;
                    };
                }
            });
        }

        /// <summary>
        /// 切换主题
        /// </summary>
        private void ToggleTheme() {
            try {
                _themeService.ToggleTheme();
                IsDarkTheme = _themeService.IsDarkTheme;

                var themeText = IsDarkTheme ? "暗色" : "浅色";
                SystemStatus = $"已切换到{themeText}主题";

                System.Diagnostics.Debug.WriteLine($"Theme switched to: {themeText}");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Theme toggle error: {ex.Message}");
                MessageBox.Show($"切换主题时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示关于系统
        /// </summary>
        private void ShowAboutSystem() {
            var aboutText = $"凌隐宝堂中医诊所管理系统\n\n" +
                           $"版本：1.0.0\n" +
                           $"构建日期：{DateTime.Now:yyyy-MM-dd}\n" +
                           $"当前用户：{CurrentUserName}\n" +
                           $"用户角色：{CurrentUserRole}\n\n" +
                           $"© 2024 凌隐宝堂中医诊所 版权所有";

            MessageBox.Show(aboutText, "关于系统", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 显示系统设置
        /// </summary>
        private void ShowSystemSettings() {
            // 这里可以实现系统设置界面
            MessageBox.Show("系统设置功能正在开发中...", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 导航到医生档案
        /// </summary>
        public void NavigateDoctorProfile(Guid? userId = null, ProfileMode? mode = null, string userName = null, string realName = null) {
            IsMainVisible = false;
            IsFunctionVisible = true;
            var actualMode = mode ?? (HasDoctorProfile ? ProfileMode.Edit : ProfileMode.Create);
            var parameters = new NavigationParameters { { "Mode", actualMode } };

            if (userId != null)
                parameters.Add("UserId", userId.Value);
            if (userName != null)
                parameters.Add("UserName", userName);
            if (realName != null)
                parameters.Add("RealName", realName);

            _regionManager.RequestNavigate("FunctionRegion", "DoctorProfileView", result => {
                var view = _regionManager.Regions["FunctionRegion"].ActiveViews.FirstOrDefault();
                if (view is FrameworkElement element && element.DataContext is DoctorProfileViewModel vm) {
                    vm.CancelAction = () => {
                        IsMainVisible = true;
                        IsFunctionVisible = false;
                    };
                }
            }, parameters);
        }

        /// <summary>
        /// 检查医生档案
        /// </summary>
        public async Task CheckDoctorProfileAsync() {
            try {
                var detail = await _doctorService.GetByUserIdAsync(_authService.UserId);
                HasDoctorProfile = detail != null;
                DoctorProfileButtonText = HasDoctorProfile ? "编辑医生档案" : "新增医生档案";
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Check doctor profile error: {ex.Message}");
                MessageBox.Show($"检查医生档案失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 更新用户信息
        /// </summary>
        private async Task UpdateUserInfoAsync(IList<UserRole> roles) {
            try {
                // 确定用户身份
                IsSysAdmin = string.Equals(_authService.RememberedUserName, "sysadmin", StringComparison.OrdinalIgnoreCase);
                IsDoctorRole = roles.Contains(UserRole.DiagnosingDoctor) || roles.Contains(UserRole.TreatmentDoctor);

                // 设置用户名
                CurrentUserName = _authService.RememberedUserName ?? "未知用户";

                // 设置用户角色显示文本
                CurrentUserRole = string.Join(", ", roles.Select(GetRoleDisplayName));

                System.Diagnostics.Debug.WriteLine($"User info updated: {CurrentUserName}, Roles: {CurrentUserRole}");
                await Task.CompletedTask;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Update user info error: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        private string GetRoleDisplayName(UserRole role) {
            return role switch {
                UserRole.Admin => "系统管理员",
                UserRole.DiagnosingDoctor => "诊疗医生",
                UserRole.TreatmentDoctor => "治疗医生",
                UserRole.PharmacyStaff => "药房人员",
                UserRole.BillingStaff => "收费人员",
                UserRole.RegistrationStaff => "挂号人员",
                _ => role.ToString()
            };
        }

        /// <summary>
        /// 更新界面状态
        /// </summary>
        private void UpdateUIState() {
            IsFunctionVisible = false;
            IsMainVisible = true;
        }

        /// <summary>
        /// 重置界面状态
        /// </summary>
        private void ResetUIState() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            IsSysAdmin = false;
            IsDoctorRole = false;
            HasDoctorProfile = false;
            CurrentUserName = "未登录";
            CurrentUserRole = "";
            CurrentUserRoles = new List<UserRole>();
            IsNavDrawerOpen = false;
            IsInitialized = false;
            SystemStatus = "系统正常";
        }

        /// <summary>
        /// 最大化窗口
        /// </summary>
        private void MaximizeWindow() {
            if (Application.Current?.MainWindow != null) {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        /// <summary>
        /// 导航到登录界面
        /// </summary>
        private void NavigateToLogin() {
            _regionManager.RequestNavigate("FunctionRegion", "LoginView");

            // 恢复窗口尺寸
            if (Application.Current?.MainWindow != null) {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Width = 420;
                Application.Current.MainWindow.Height = 480;
            }
        }

        /// <summary>
        /// 导航到主页
        /// </summary>
        private async Task NavigateToHomeAsync(IList<UserRole> roles) {
            var homeNavigationParams = new NavigationParameters();
            homeNavigationParams.Add("UserRoles", roles);

            _regionManager.RequestNavigate("MainContentRegion", "HomeView", result => {
                System.Diagnostics.Debug.WriteLine($"Navigation to HomeView completed. Success: {result.Exception == null}");
                if (result.Exception != null) {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {result.Exception.Message}");
                } else {
                    System.Diagnostics.Debug.WriteLine("Navigation to HomeView successful!");
                }
            }, homeNavigationParams);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 刷新系统状态
        /// </summary>
        private async Task RefreshSystemStatusAsync() {
            try {
                // 这里可以添加系统状态检查逻辑
                // 例如：检查服务器连接、数据库状态等
                SystemStatus = "系统运行正常";

                await Task.CompletedTask; // 占位符，实际可以调用相关服务
            } catch (Exception ex) {
                SystemStatus = $"系统状态异常：{ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Refresh system status error: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup() {
            _statusTimer?.Stop();
        }

        #endregion
    }
}