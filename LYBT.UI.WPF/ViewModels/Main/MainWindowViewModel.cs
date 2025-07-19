using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.Common.Enums;
using System.Windows;
using System.Windows.Threading;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 重构后的主窗口视图模型 - 修复退出登录后界面恢复问题
    /// </summary>
    public class MainWindowViewModel : BindableBase {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IAuthService _authService;
        private readonly IDoctorService _doctorService;
        private readonly IThemeService _themeService;

        // 保存登录前的窗口状态
        private WindowState _originalWindowState;
        private double _originalWidth;
        private double _originalHeight;
        private double _originalLeft;
        private double _originalTop;

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

        private string _currentUserName = "未登录";
        /// <summary>
        /// 当前用户名（用于标题栏显示）
        /// </summary>
        public string CurrentUserName {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        private string _systemStatus = "系统正常";
        /// <summary>
        /// 系统状态（用于标题栏显示）
        /// </summary>
        public string SystemStatus {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>
        /// 当前时间（用于标题栏显示）
        /// </summary>
        public string CurrentTime {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        private bool _isInitialized = false;
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        private bool _isNavDrawerOpen;
        /// <summary>
        /// 导航抽屉是否打开（用于兼容原有XAML绑定）
        /// </summary>
        public bool IsNavDrawerOpen {
            get => _isNavDrawerOpen;
            set => SetProperty(ref _isNavDrawerOpen, value);
        }

        // 保留一些原有属性以确保向后兼容
        private bool _isDoctorRole = false;
        public bool IsDoctorRole {
            get => _isDoctorRole;
            set => SetProperty(ref _isDoctorRole, value);
        }

        private bool _isSysAdmin;
        public bool IsSysAdmin {
            get => _isSysAdmin;
            set => SetProperty(ref _isSysAdmin, value);
        }

        private bool _hasDoctorProfile;
        public bool HasDoctorProfile {
            get => _hasDoctorProfile;
            set => SetProperty(ref _hasDoctorProfile, value);
        }

        private string _doctorProfileButtonText = "新增医生档案";
        public string DoctorProfileButtonText {
            get => _doctorProfileButtonText;
            set => SetProperty(ref _doctorProfileButtonText, value);
        }

        public bool IsNotSysAdmin => !IsSysAdmin;

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

        #endregion

        #region Commands - 保留原有命令以确保兼容性

        public DelegateCommand LogoutCommand { get; private set; }
        public DelegateCommand ShowChangePasswordCommand { get; private set; }
        public DelegateCommand ShowChangeProfileCommand { get; private set; }
        public DelegateCommand ShowDoctorProfileCommand { get; private set; }
        public DelegateCommand ShowPatientProfileCommand { get; private set; }
        public DelegateCommand ToggleNavDrawerCommand { get; private set; }
        public DelegateCommand ToggleThemeCommand { get; private set; }
        public DelegateCommand RefreshStatusCommand { get; private set; }
        public DelegateCommand AboutSystemCommand { get; private set; }
        public DelegateCommand SystemSettingsCommand { get; private set; }

        #endregion

        private DispatcherTimer _statusTimer;

        public MainWindowViewModel(IEventAggregator eventAggregator, IRegionManager regionManager,
                                   IAuthService authService, IDoctorService doctorService,
                                   IThemeService themeService) {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            // 保存初始窗口状态
            SaveInitialWindowState();

            // 初始化命令（保持向后兼容）
            InitializeCommands();

            // 订阅事件
            SubscribeToEvents();

            // 初始化状态定时器
            InitializeStatusTimer();

            System.Diagnostics.Debug.WriteLine("Enhanced MainWindowViewModel constructed with logout fix");
        }

        #region Initialization

        private void SaveInitialWindowState() {
            try {
                if (Application.Current?.MainWindow != null) {
                    var window = Application.Current.MainWindow;
                    _originalWindowState = window.WindowState;
                    _originalWidth = window.Width;
                    _originalHeight = window.Height;
                    _originalLeft = window.Left;
                    _originalTop = window.Top;

                    System.Diagnostics.Debug.WriteLine($"Saved initial window state: {_originalWindowState}, Size: {_originalWidth}x{_originalHeight}, Position: {_originalLeft},{_originalTop}");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"SaveInitialWindowState error: {ex.Message}");
                // 设置默认值
                _originalWindowState = WindowState.Normal;
                _originalWidth = 420;
                _originalHeight = 480;
                _originalLeft = 0;
                _originalTop = 0;
            }
        }

        private void InitializeCommands() {
            // 保留原有命令，但通过事件系统实现
            LogoutCommand = new DelegateCommand(() => {
                _eventAggregator.GetEvent<LogoutEvent>().Publish();
            });

            ShowChangePasswordCommand = new DelegateCommand(() => {
                _eventAggregator.GetEvent<NavigateToFunctionEvent>().Publish("ChangePasswordView");
            });

            ShowChangeProfileCommand = new DelegateCommand(() => {
                _eventAggregator.GetEvent<NavigateToFunctionEvent>().Publish("ChangeProfileView");
            });

            ShowDoctorProfileCommand = new DelegateCommand(() => {
                _eventAggregator.GetEvent<NavigateToDoctorProfileEvent>().Publish(new DoctorProfileNavigationArgs {
                    Mode = HasDoctorProfile ? ProfileMode.Edit : ProfileMode.Create
                });
            });

            ShowPatientProfileCommand = new DelegateCommand(() => {
                _eventAggregator.GetEvent<NavigateToFunctionEvent>().Publish("PatientProfileView");
            });

            ToggleNavDrawerCommand = new DelegateCommand(() => {
                IsNavDrawerOpen = !IsNavDrawerOpen;
            });

            ToggleThemeCommand = new DelegateCommand(() => {
                try {
                    _themeService.ToggleTheme();
                    var themeText = _themeService.IsDarkTheme ? "暗色" : "浅色";
                    SystemStatus = $"已切换到{themeText}主题";
                    _eventAggregator.GetEvent<ThemeChangedEvent>().Publish(themeText);
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Theme toggle error: {ex.Message}");
                    SystemStatus = $"主题切换失败：{ex.Message}";
                }
            });

            RefreshStatusCommand = new DelegateCommand(async () => await RefreshSystemStatusAsync());

            AboutSystemCommand = new DelegateCommand(ShowAboutSystem);

            SystemSettingsCommand = new DelegateCommand(ShowSystemSettings);
        }

        private void SubscribeToEvents() {
            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 订阅退出登录事件
            _eventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout);

            // 订阅功能界面导航事件
            _eventAggregator.GetEvent<NavigateToFunctionEvent>().Subscribe(OnNavigateToFunction);

            // 订阅医生档案导航事件
            _eventAggregator.GetEvent<NavigateToDoctorProfileEvent>().Subscribe(OnNavigateToDoctorProfile);

            // 订阅系统状态更新事件
            _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Subscribe(OnSystemStatusUpdated);
        }

        private void InitializeStatusTimer() {
            _statusTimer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusTimer.Tick += StatusTimer_Tick;
        }

        #endregion

        #region Event Handlers

        private void StatusTimer_Tick(object sender, EventArgs e) {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private async void OnLoginSuccess(IList<UserRole> roles) {
            try {
                System.Diagnostics.Debug.WriteLine($"OnLoginSuccess called with roles: {string.Join(", ", roles)}");

                // 更新用户信息（保持原有逻辑）
                await UpdateUserInfoAsync(roles);

                // 更新界面状态 - 切换到主界面并最大化窗口
                UpdateUIStateForLogin();

                // 启动状态定时器
                _statusTimer.Start();

                // 导航到整合后的主布局
                await NavigateToIntegratedMainLayoutAsync(roles);

                // 标记为已初始化
                IsInitialized = true;

                SystemStatus = "系统初始化完成";

                System.Diagnostics.Debug.WriteLine("Main window initialization completed successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnLoginSuccess error: {ex.Message}");
                SystemStatus = $"初始化失败：{ex.Message}";
                MessageBox.Show($"初始化主界面时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnLogout() {
            try {
                System.Diagnostics.Debug.WriteLine("OnLogout: Starting logout process");

                // 执行原有的登出逻辑
                _statusTimer?.Stop();
                ResetUIState();

                // 恢复窗口到登录前的状态
                RestoreLoginWindowState();

                // 导航回登录界面
                NavigateToLogin();

                System.Diagnostics.Debug.WriteLine("User logged out successfully with window state restored");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnLogout error: {ex.Message}");
            }
        }

        private void OnNavigateToFunction(string functionView) {
            // 切换到功能区域
            IsFunctionVisible = true;
            IsMainVisible = false;
        }

        private void OnNavigateToDoctorProfile(DoctorProfileNavigationArgs args) {
            // 调用原有的导航方法，保持兼容性
            NavigateDoctorProfile(args.UserId, args.Mode, args.UserName, args.RealName);
        }

        private void OnSystemStatusUpdated(string status) {
            SystemStatus = status;
        }

        #endregion

        #region Public Methods - 保留原有方法以确保兼容性

        /// <summary>
        /// 导航到医生档案 - 保留原有方法
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
        /// 检查医生档案 - 保留原有方法
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
        /// 刷新系统状态
        /// </summary>
        private async Task RefreshSystemStatusAsync() {
            try {
                SystemStatus = "正在刷新系统状态...";

                // 这里可以添加实际的系统状态检查逻辑
                await Task.Delay(1000); // 模拟检查过程

                SystemStatus = "系统运行正常";
                _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish("系统状态已刷新");
            } catch (Exception ex) {
                SystemStatus = $"系统状态异常：{ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Refresh system status error: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示关于系统
        /// </summary>
        private void ShowAboutSystem() {
            var aboutText = $"凌隐宝堂中医诊所管理系统\n\n" +
                           $"版本：1.0.0\n" +
                           $"构建日期：{DateTime.Now:yyyy-MM-dd}\n" +
                           $"当前用户：{CurrentUserName}\n\n" +
                           $"© 2024 凌隐宝堂中医诊所 版权所有";

            MessageBox.Show(aboutText, "关于系统", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 显示系统设置
        /// </summary>
        private void ShowSystemSettings() {
            MessageBox.Show("系统设置功能正在开发中...", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 更新用户信息 - 保留原有逻辑
        /// </summary>
        private async Task UpdateUserInfoAsync(IList<UserRole> roles) {
            try {
                CurrentUserRoles = roles;
                IsSysAdmin = string.Equals(_authService.RememberedUserName, "sysadmin", StringComparison.OrdinalIgnoreCase);
                IsDoctorRole = roles.Contains(UserRole.DiagnosingDoctor) || roles.Contains(UserRole.TreatmentDoctor);
                CurrentUserName = _authService.RememberedUserName ?? "未知用户";

                // 更新主题状态
                IsDarkTheme = _themeService.IsDarkTheme;

                if (IsDoctorRole) {
                    await CheckDoctorProfileAsync();
                }

                System.Diagnostics.Debug.WriteLine($"User info updated: {CurrentUserName}");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Update user info error: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新界面状态 - 登录后切换到主界面并最大化
        /// </summary>
        private void UpdateUIStateForLogin() {
            IsFunctionVisible = false;
            IsMainVisible = true;

            // 最大化窗口
            MaximizeWindow();
        }

        /// <summary>
        /// 重置界面状态 - 退出登录时恢复
        /// </summary>
        private void ResetUIState() {
            IsMainVisible = false;
            IsFunctionVisible = true;
            CurrentUserName = "未登录";
            IsSysAdmin = false;
            IsDoctorRole = false;
            HasDoctorProfile = false;
            IsInitialized = false;
            IsNavDrawerOpen = false;
            CurrentUserRoles = new List<UserRole>();
            DoctorProfileButtonText = "新增医生档案";
            SystemStatus = "系统正常";
        }

        /// <summary>
        /// 最大化窗口
        /// </summary>
        private void MaximizeWindow() {
            try {
                if (Application.Current?.MainWindow != null) {
                    Application.Current.MainWindow.WindowState = WindowState.Maximized;
                    System.Diagnostics.Debug.WriteLine("Window maximized after login");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"MaximizeWindow error: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复登录窗口状态 - 关键修复方法
        /// </summary>
        private void RestoreLoginWindowState() {
            try {
                if (Application.Current?.MainWindow != null) {
                    var window = Application.Current.MainWindow;

                    // 恢复到登录前的窗口状态
                    window.WindowState = WindowState.Maximized;
                    //window.WindowState = _originalWindowState;

                    // 如果原来是普通窗口，恢复尺寸和位置
                    if (_originalWindowState == WindowState.Normal) {
                        window.Width = _originalWidth > 0 ? _originalWidth : 420;
                        window.Height = _originalHeight > 0 ? _originalHeight : 480;

                        // 居中显示
                        window.Left = (SystemParameters.PrimaryScreenWidth - window.Width) / 2;
                        window.Top = (SystemParameters.PrimaryScreenHeight - window.Height) / 2;
                    }

                    System.Diagnostics.Debug.WriteLine($"Window state restored to: {window.WindowState}, Size: {window.Width}x{window.Height}");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"RestoreLoginWindowState error: {ex.Message}");

                // 如果恢复失败，至少确保窗口回到合理的登录状态
                try {
                    if (Application.Current?.MainWindow != null) {
                        var window = Application.Current.MainWindow;
                        window.WindowState = WindowState.Normal;
                        window.Width = 420;
                        window.Height = 480;
                        window.Left = (SystemParameters.PrimaryScreenWidth - window.Width) / 2;
                        window.Top = (SystemParameters.PrimaryScreenHeight - window.Height) / 2;

                        System.Diagnostics.Debug.WriteLine("Window state restored to default login size");
                    }
                } catch (Exception innerEx) {
                    System.Diagnostics.Debug.WriteLine($"Fallback window restore failed: {innerEx.Message}");
                }
            }
        }

        /// <summary>
        /// 导航到登录界面
        /// </summary>
        private void NavigateToLogin() {
            _regionManager.RequestNavigate("FunctionRegion", "LoginView");
        }

        /// <summary>
        /// 导航到整合后的主布局
        /// </summary>
        private async Task NavigateToIntegratedMainLayoutAsync(IList<UserRole> roles) {
            _regionManager.RequestNavigate("MainContentRegion", "IntegratedMainLayout", result => {
                System.Diagnostics.Debug.WriteLine($"Navigation to IntegratedMainLayout completed. Success: {result.Exception == null}");
                if (result.Exception != null) {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {result.Exception.Message}");
                } else {
                    System.Diagnostics.Debug.WriteLine("Navigation to IntegratedMainLayout successful!");
                    // 发布用户信息更新事件
                    _eventAggregator.GetEvent<UserInfoUpdatedEvent>().Publish(roles);
                }
            });

            await Task.CompletedTask;
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