using LYBT.Common.Enums;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LYBT.UI.WPF.ViewModels.Components {
    /// <summary>
    /// 增强的欢迎面板视图模型 - 整合用户菜单功能
    /// </summary>
    public class WelcomePanelViewModel : BindableBase {
        private readonly IEventAggregator _eventAggregator;
        private readonly IAuthService _authService;
        private readonly IDoctorService _doctorService;
        private readonly IThemeService _themeService;
        private DispatcherTimer _timer;

        #region Properties

        private string _welcomeMessage = "欢迎使用凌隐宝堂中医诊所管理系统";
        /// <summary>
        /// 欢迎消息
        /// </summary>
        public string WelcomeMessage {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private string _currentDateTime = DateTime.Now.ToString("yyyy年MM月dd日 dddd HH:mm");
        /// <summary>
        /// 当前日期时间
        /// </summary>
        public string CurrentDateTime {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        private bool _hasNavigationItems = false;
        /// <summary>
        /// 是否有导航项
        /// </summary>
        public bool HasNavigationItems {
            get => _hasNavigationItems;
            set => SetProperty(ref _hasNavigationItems, value);
        }

        private IList<UserRole> _currentRoles = new List<UserRole>();
        /// <summary>
        /// 当前用户角色
        /// </summary>
        public IList<UserRole> CurrentRoles {
            get => _currentRoles;
            set => SetProperty(ref _currentRoles, value);
        }

        // 用户信息相关属性
        private string _currentUserName = "未登录";
        public string CurrentUserName {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        private string _currentUserRole = "";
        public string CurrentUserRole {
            get => _currentUserRole;
            set => SetProperty(ref _currentUserRole, value);
        }

        private bool _isSysAdmin;
        public bool IsSysAdmin {
            get => _isSysAdmin;
            set {
                if (SetProperty(ref _isSysAdmin, value)) {
                    RaisePropertyChanged(nameof(IsNotSysAdmin));
                }
            }
        }

        public bool IsNotSysAdmin => !IsSysAdmin;

        private bool _isDoctorRole = false;
        public bool IsDoctorRole {
            get => _isDoctorRole;
            set => SetProperty(ref _isDoctorRole, value);
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

        #endregion

        #region Commands

        /// <summary>
        /// 打开导航菜单命令
        /// </summary>
        public DelegateCommand OpenNavMenuCommand { get; private set; }

        /// <summary>
        /// 刷新菜单命令
        /// </summary>
        public DelegateCommand RefreshMenuCommand { get; private set; }

        // 用户菜单相关命令
        public DelegateCommand ShowChangePasswordCommand { get; private set; }
        public DelegateCommand ShowChangeProfileCommand { get; private set; }
        public DelegateCommand ShowDoctorProfileCommand { get; private set; }
        public DelegateCommand ToggleThemeCommand { get; private set; }
        public DelegateCommand SystemSettingsCommand { get; private set; }
        public DelegateCommand AboutSystemCommand { get; private set; }
        public DelegateCommand LogoutCommand { get; private set; }

        #endregion

        public WelcomePanelViewModel(IEventAggregator eventAggregator, IAuthService authService,
                                    IDoctorService doctorService, IThemeService themeService) {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            InitializeCommands();
            InitializeTimer();
        }

        #region Private Methods

        private void InitializeCommands() {
            // 原有命令
            OpenNavMenuCommand = new DelegateCommand(() => {
                _eventAggregator.GetEvent<ToggleNavDrawerEvent>().Publish();
            });

            RefreshMenuCommand = new DelegateCommand(() => {
                _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish("正在刷新菜单...");
            });

            // 用户菜单命令
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

            ToggleThemeCommand = new DelegateCommand(() => {
                try {
                    _themeService.ToggleTheme();
                    var themeText = _themeService.IsDarkTheme ? "暗色" : "浅色";
                    _eventAggregator.GetEvent<ThemeChangedEvent>().Publish(themeText);
                    _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish($"已切换到{themeText}主题");
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Theme toggle error: {ex.Message}");
                    MessageBox.Show($"切换主题时发生错误：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            SystemSettingsCommand = new DelegateCommand(() => {
                MessageBox.Show("系统设置功能正在开发中...", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });

            AboutSystemCommand = new DelegateCommand(() => {
                var aboutText = $"凌隐宝堂中医诊所管理系统\n\n" +
                               $"版本：1.0.0\n" +
                               $"构建日期：{DateTime.Now:yyyy-MM-dd}\n" +
                               $"当前用户：{CurrentUserName}\n" +
                               $"用户角色：{CurrentUserRole}\n\n" +
                               $"© 2024 凌隐宝堂中医诊所 版权所有";

                MessageBox.Show(aboutText, "关于系统", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            LogoutCommand = new DelegateCommand(async () => await LogoutAsync());
        }

        private void InitializeTimer() {
            _timer = new DispatcherTimer {
                Interval = TimeSpan.FromMinutes(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            CurrentDateTime = DateTime.Now.ToString("yyyy年MM月dd日 dddd HH:mm");
        }

        private async Task LogoutAsync() {
            try {
                var result = MessageBox.Show(
                    "确定要退出登录吗？",
                    "退出确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                await _authService.LogoutAsync();
                _eventAggregator.GetEvent<LogoutEvent>().Publish();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
                MessageBox.Show($"退出登录时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 更新用户信息 - 主要的同步方法（推荐使用）
        /// </summary>
        public void UpdateUserInfo(IList<UserRole> roles) {
            try {
                CurrentRoles = roles;
                HasNavigationItems = roles?.Count > 0;

                // 更新用户信息
                IsSysAdmin = string.Equals(_authService.RememberedUserName, "sysadmin", StringComparison.OrdinalIgnoreCase);
                IsDoctorRole = roles.Contains(UserRole.DiagnosingDoctor) || roles.Contains(UserRole.TreatmentDoctor);
                CurrentUserName = _authService.RememberedUserName ?? "未知用户";
                CurrentUserRole = string.Join(", ", roles.Select(GetRoleDisplayName));

                UpdateWelcomeMessage();

                // 异步检查医生档案（不阻塞UI）
                if (IsDoctorRole) {
                    _ = Task.Run(async () => await CheckDoctorProfileAsync());
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"WelcomePanel UpdateUserInfo error: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户信息 - 异步版本（用于需要等待医生档案检查的场景）
        /// </summary>
        public async Task UpdateUserInfoAsync(IList<UserRole> roles) {
            try {
                CurrentRoles = roles;
                HasNavigationItems = roles?.Count > 0;

                // 更新用户信息
                IsSysAdmin = string.Equals(_authService.RememberedUserName, "sysadmin", StringComparison.OrdinalIgnoreCase);
                IsDoctorRole = roles.Contains(UserRole.DiagnosingDoctor) || roles.Contains(UserRole.TreatmentDoctor);
                CurrentUserName = _authService.RememberedUserName ?? "未知用户";
                CurrentUserRole = string.Join(", ", roles.Select(GetRoleDisplayName));

                UpdateWelcomeMessage();

                // 同步等待医生档案检查
                if (IsDoctorRole) {
                    await CheckDoctorProfileAsync();
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"WelcomePanel UpdateUserInfoAsync error: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset() {
            WelcomeMessage = "欢迎使用凌隐宝堂中医诊所管理系统";
            CurrentRoles = new List<UserRole>();
            HasNavigationItems = false;
            CurrentUserName = "未登录";
            CurrentUserRole = "";
            IsSysAdmin = false;
            IsDoctorRole = false;
            HasDoctorProfile = false;
        }

        /// <summary>
        /// 检查医生档案
        /// </summary>
        private async Task CheckDoctorProfileAsync() {
            try {
                var detail = await _doctorService.GetByUserIdAsync(_authService.UserId);
                HasDoctorProfile = detail != null;
                DoctorProfileButtonText = HasDoctorProfile ? "编辑医生档案" : "新增医生档案";

                System.Diagnostics.Debug.WriteLine($"Doctor profile check completed. HasProfile: {HasDoctorProfile}");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Check doctor profile error: {ex.Message}");
                // 设置默认值，避免UI显示异常
                HasDoctorProfile = false;
                DoctorProfileButtonText = "新增医生档案";
            }
        }

        /// <summary>
        /// 更新欢迎消息
        /// </summary>
        private void UpdateWelcomeMessage() {
            var timeGreeting = GetTimeGreeting();
            var roleText = CurrentRoles.Any() ?
                $"，您的角色是：{string.Join("、", CurrentRoles.Select(GetRoleDisplayName))}" : "";

            WelcomeMessage = $"{timeGreeting}！欢迎使用凌隐宝堂中医诊所管理系统{roleText}";
        }

        /// <summary>
        /// 获取时间问候语
        /// </summary>
        private string GetTimeGreeting() {
            var hour = DateTime.Now.Hour;
            return hour switch {
                >= 6 and < 12 => "早上好",
                >= 12 and < 14 => "中午好",
                >= 14 and < 18 => "下午好",
                >= 18 and < 22 => "晚上好",
                _ => "夜深了"
            };
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

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup() {
            _timer?.Stop();
        }

        #endregion
    }
}