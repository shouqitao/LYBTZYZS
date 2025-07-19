using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Models;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 重构后的主内容区ViewModel - 增强导航和状态管理
    /// </summary>
    public class HomeViewModel : BindableBase, INavigationAware {
        private readonly IRegionManager _regionManager;
        private readonly MainWindowViewModel _mainWindow;
        private readonly DispatcherTimer _navigationTimer;

        #region Properties

        /// <summary>
        /// 导航菜单项集合
        /// </summary>
        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

        /// <summary>
        /// 快速访问菜单项
        /// </summary>
        public ObservableCollection<NavigationItem> QuickAccessItems { get; } = new();

        /// <summary>
        /// 最近访问的菜单项
        /// </summary>
        public ObservableCollection<NavigationItem> RecentItems { get; } = new();

        private NavigationItem _selectedNavigationItem;
        /// <summary>
        /// 当前选中的导航项
        /// </summary>
        public NavigationItem SelectedNavigationItem {
            get => _selectedNavigationItem;
            set => SetProperty(ref _selectedNavigationItem, value);
        }

        private IList<UserRole> _currentRoles = new List<UserRole>();
        /// <summary>
        /// 当前用户角色
        /// </summary>
        public IList<UserRole> CurrentRoles {
            get => _currentRoles;
            set => SetProperty(ref _currentRoles, value);
        }

        private string _welcomeMessage = "欢迎使用凌隐宝堂中医诊所管理系统";
        /// <summary>
        /// 欢迎消息
        /// </summary>
        public string WelcomeMessage {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private string _currentDateTime = DateTime.Now.ToString("yyyy年MM月dd日 dddd");
        /// <summary>
        /// 当前日期时间
        /// </summary>
        public string CurrentDateTime {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        private bool _isLoading = false;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = "准备就绪";
        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private int _navigationCount = 0;
        /// <summary>
        /// 导航计数
        /// </summary>
        public int NavigationCount {
            get => _navigationCount;
            set => SetProperty(ref _navigationCount, value);
        }

        private bool _hasNavigationItems = false;
        /// <summary>
        /// 是否有导航项
        /// </summary>
        public bool HasNavigationItems {
            get => _hasNavigationItems;
            set => SetProperty(ref _hasNavigationItems, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 导航命令
        /// </summary>
        public DelegateCommand<NavigationItem> NavigateCommand { get; private set; }

        /// <summary>
        /// 刷新菜单命令
        /// </summary>
        public DelegateCommand RefreshMenuCommand { get; private set; }

        /// <summary>
        /// 清除最近访问命令
        /// </summary>
        public DelegateCommand ClearRecentCommand { get; private set; }

        /// <summary>
        /// 添加到快速访问命令
        /// </summary>
        public DelegateCommand<NavigationItem> AddToQuickAccessCommand { get; private set; }

        /// <summary>
        /// 从快速访问移除命令
        /// </summary>
        public DelegateCommand<NavigationItem> RemoveFromQuickAccessCommand { get; private set; }

        #endregion

        public HomeViewModel(IRegionManager regionManager) {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 安全获取MainWindow的DataContext
            if (Application.Current?.MainWindow?.DataContext is MainWindowViewModel mainWindowViewModel) {
                _mainWindow = mainWindowViewModel;
            }

            // 初始化命令
            InitializeCommands();

            // 初始化导航定时器
            InitializeNavigationTimer();

            System.Diagnostics.Debug.WriteLine("Enhanced HomeViewModel constructed");
        }

        #region Initialization

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands() {
            NavigateCommand = new DelegateCommand<NavigationItem>(async (item) => await NavigateAsync(item));
            RefreshMenuCommand = new DelegateCommand(async () => await RefreshMenuAsync());
            ClearRecentCommand = new DelegateCommand(ClearRecentItems);
            AddToQuickAccessCommand = new DelegateCommand<NavigationItem>(AddToQuickAccess);
            RemoveFromQuickAccessCommand = new DelegateCommand<NavigationItem>(RemoveFromQuickAccess);
        }

        /// <summary>
        /// 初始化导航定时器
        /// </summary>
        private void InitializeNavigationTimer() {
            _navigationTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMinutes(1)
            };
            _navigationTimer.Tick += NavigationTimer_Tick;
        }

        #endregion

        #region Navigation Interface Implementation

        /// <summary>
        /// 导航到此视图时调用
        /// </summary>
        public async void OnNavigatedTo(NavigationContext navigationContext) {
            try {
                System.Diagnostics.Debug.WriteLine("HomeViewModel.OnNavigatedTo called");

                IsLoading = true;
                StatusMessage = "正在初始化...";

                // 获取用户角色
                if (navigationContext.Parameters.TryGetValue("UserRoles", out IList<UserRole> roles)) {
                    System.Diagnostics.Debug.WriteLine($"Found {roles.Count} user roles: {string.Join(", ", roles)}");
                    CurrentRoles = roles;
                    await LoadNavigationAsync(roles);
                } else {
                    System.Diagnostics.Debug.WriteLine("No UserRoles found in navigation parameters");
                    await LoadDefaultNavigationAsync();
                }

                // 更新欢迎信息
                UpdateWelcomeMessage();

                // 启动定时器
                _navigationTimer.Start();

                StatusMessage = "初始化完成";
                IsLoading = false;

                System.Diagnostics.Debug.WriteLine($"HomeViewModel initialization completed. Navigation items: {NavigationItems.Count}");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"HomeViewModel.OnNavigatedTo error: {ex.Message}");
                StatusMessage = $"初始化失败：{ex.Message}";
                IsLoading = false;
            }
        }

        /// <summary>
        /// 判断是否为导航目标
        /// </summary>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>
        /// 从此视图导航离开时调用
        /// </summary>
        public void OnNavigatedFrom(NavigationContext navigationContext) {
            System.Diagnostics.Debug.WriteLine("HomeViewModel.OnNavigatedFrom called");
            _navigationTimer?.Stop();
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// 执行导航
        /// </summary>
        private async Task NavigateAsync(NavigationItem item) {
            if (item == null)
                return;

            try {
                SelectedNavigationItem = item;
                StatusMessage = $"正在导航到 {item.DisplayName}...";

                // 检查目标视图是否存在
                var viewType = typeof(HomeViewModel).Assembly.GetType($"LYBT.UI.WPF.Views.Navigation.{item.TargetView}");
                if (viewType == null) {
                    MessageBox.Show($"功能 [{item.DisplayName}] 暂未开放或未实现。",
                        "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusMessage = "导航取消";
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Navigating to: {item.TargetView}");

                // 执行导航
                _regionManager.RequestNavigate("ContentRegion", item.TargetView, navigationResult => {
                    Application.Current.Dispatcher.Invoke(() => {
                        if (navigationResult.Result == true) {
                            StatusMessage = $"已切换到 {item.DisplayName}";
                            AddToRecentItems(item);
                            NavigationCount++;
                        } else {
                            StatusMessage = "导航失败";
                            System.Diagnostics.Debug.WriteLine($"Navigation failed: {navigationResult.Exception?.Message}");
                        }
                    });
                });

                // 关闭导航抽屉
                if (_mainWindow != null) {
                    _mainWindow.IsNavDrawerOpen = false;
                }

                await Task.Delay(100); // 短暂延迟以显示状态消息
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                StatusMessage = $"导航错误：{ex.Message}";
                MessageBox.Show($"导航到 {item.DisplayName} 时发生错误：{ex.Message}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新菜单
        /// </summary>
        private async Task RefreshMenuAsync() {
            try {
                StatusMessage = "正在刷新菜单...";
                await LoadNavigationAsync(CurrentRoles);
                StatusMessage = "菜单已刷新";
            } catch (Exception ex) {
                StatusMessage = $"刷新菜单失败：{ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Refresh menu error: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除最近访问
        /// </summary>
        private void ClearRecentItems() {
            RecentItems.Clear();
            StatusMessage = "最近访问已清除";
        }

        /// <summary>
        /// 添加到快速访问
        /// </summary>
        private void AddToQuickAccess(NavigationItem item) {
            if (item == null || QuickAccessItems.Any(q => q.TargetView == item.TargetView))
                return;

            QuickAccessItems.Add(new NavigationItem(item.DisplayName, item.TargetView, item.Icon));
            StatusMessage = $"已添加 {item.DisplayName} 到快速访问";
        }

        /// <summary>
        /// 从快速访问移除
        /// </summary>
        private void RemoveFromQuickAccess(NavigationItem item) {
            if (item == null)
                return;

            var itemToRemove = QuickAccessItems.FirstOrDefault(q => q.TargetView == item.TargetView);
            if (itemToRemove != null) {
                QuickAccessItems.Remove(itemToRemove);
                StatusMessage = $"已从快速访问移除 {item.DisplayName}";
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 定时器事件处理
        /// </summary>
        private void NavigationTimer_Tick(object sender, EventArgs e) {
            CurrentDateTime = DateTime.Now.ToString("yyyy年MM月dd日 dddd HH:mm");
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

        /// <summary>
        /// 动态加载导航菜单
        /// </summary>
        private async Task LoadNavigationAsync(IEnumerable<UserRole> roles) {
            try {
                System.Diagnostics.Debug.WriteLine($"LoadNavigation called with roles: {string.Join(", ", roles)}");

                NavigationItems.Clear();

                foreach (var role in roles.OrderBy(r => (int)r)) {
                    var items = GetNavigationItemsForRole(role);
                    foreach (var item in items) {
                        if (!NavigationItems.Any(n => n.TargetView == item.TargetView)) {
                            NavigationItems.Add(item);
                        }
                    }
                }

                HasNavigationItems = NavigationItems.Count > 0;

                System.Diagnostics.Debug.WriteLine($"NavigationItems count after loading: {NavigationItems.Count}");

                await Task.CompletedTask;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"LoadNavigation error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载默认导航
        /// </summary>
        private async Task LoadDefaultNavigationAsync() {
            NavigationItems.Clear();
            NavigationItems.Add(new NavigationItem("默认功能模块", "DefaultView", "Home"));
            HasNavigationItems = true;
            System.Diagnostics.Debug.WriteLine("Loaded default navigation items");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 根据角色获取导航项
        /// </summary>
        private List<NavigationItem> GetNavigationItemsForRole(UserRole role) {
            return role switch {
                UserRole.Admin => new List<NavigationItem> {
                    new("系统管理", "AdminView", "Settings"),
                    new("数据统计", "StatisticsView", "ChartLine"),
                    new("系统日志", "LogView", "FileText")
                },
                UserRole.DiagnosingDoctor => new List<NavigationItem> {
                    new("诊疗工作台", "DiagnosingDoctorView", "Stethoscope"),
                    new("患者病历", "PatientRecordsView", "FolderAccount"),
                    new("处方管理", "PrescriptionView", "Prescription")
                },
                UserRole.TreatmentDoctor => new List<NavigationItem> {
                    new("治疗工作台", "TreatmentDoctorView", "HospitalBox"),
                    new("治疗计划", "TreatmentPlanView", "CalendarCheck"),
                    new("康复跟踪", "RehabilitationView", "TrendingUp")
                },
                UserRole.PharmacyStaff => new List<NavigationItem> {
                    new("药房工作台", "PharmacyStaffView", "Pill"),
                    new("药品库存", "InventoryView", "PackageVariant"),
                    new("配药记录", "DispensingView", "ClipboardList")
                },
                UserRole.BillingStaff => new List<NavigationItem> {
                    new("收费工作台", "BillingStaffView", "CashRegister"),
                    new("账单管理", "BillManagementView", "Receipt"),
                    new("财务报表", "FinancialReportView", "ChartBar")
                },
                UserRole.RegistrationStaff => new List<NavigationItem> {
                    new("挂号工作台", "RegistrationStaffView", "AccountPlus"),
                    new("患者管理", "PatientManagementView", "AccountGroup"),
                    new("排队管理", "QueueManagementView", "FormatListNumbered")
                },
                _ => new List<NavigationItem>()
            };
        }

        /// <summary>
        /// 添加到最近访问
        /// </summary>
        private void AddToRecentItems(NavigationItem item) {
            // 移除重复项
            var existing = RecentItems.FirstOrDefault(r => r.TargetView == item.TargetView);
            if (existing != null) {
                RecentItems.Remove(existing);
            }

            // 添加到开头
            RecentItems.Insert(0, new NavigationItem(item.DisplayName, item.TargetView, item.Icon));

            // 限制最近访问项目数量
            while (RecentItems.Count > 10) {
                RecentItems.RemoveAt(RecentItems.Count - 1);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup() {
            _navigationTimer?.Stop();
        }

        #endregion
    }
}