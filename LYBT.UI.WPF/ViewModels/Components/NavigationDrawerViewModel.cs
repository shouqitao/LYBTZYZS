using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Components {
    /// <summary>
    /// 导航抽屉视图模型
    /// </summary>
    public class NavigationDrawerViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;

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

        public NavigationDrawerViewModel(IRegionManager regionManager, IEventAggregator eventAggregator) {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            InitializeCommands();
        }

        #region Private Methods

        private void InitializeCommands() {
            NavigateCommand = new DelegateCommand<NavigationItem>(async (item) => await NavigateAsync(item));
            RefreshMenuCommand = new DelegateCommand(async () => await RefreshMenuAsync());
            ClearRecentCommand = new DelegateCommand(ClearRecentItems);
            AddToQuickAccessCommand = new DelegateCommand<NavigationItem>(AddToQuickAccess);
            RemoveFromQuickAccessCommand = new DelegateCommand<NavigationItem>(RemoveFromQuickAccess);
        }

        /// <summary>
        /// 执行导航
        /// </summary>
        private async Task NavigateAsync(NavigationItem item) {
            if (item == null)
                return;

            try {
                SelectedNavigationItem = item;

                // 检查目标视图是否存在
                var viewType = typeof(NavigationDrawerViewModel).Assembly.GetType($"LYBT.UI.WPF.Views.Navigation.{item.TargetView}");
                if (viewType == null) {
                    MessageBox.Show($"功能 [{item.DisplayName}] 暂未开放或未实现。",
                        "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Navigating to: {item.TargetView}");

                // 执行导航
                _regionManager.RequestNavigate("IntegratedContentRegion", item.TargetView, navigationResult => {
                    Application.Current.Dispatcher.Invoke(() => {
                        if (navigationResult.Success) {
                            AddToRecentItems(item);
                            NavigationCount++;
                            _eventAggregator.GetEvent<NavigationCompletedEvent>().Publish(item.TargetView);
                            _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish($"已切换到 {item.DisplayName}");
                        } else {
                            System.Diagnostics.Debug.WriteLine($"Navigation failed: {navigationResult.Exception?.Message}");
                            _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish("导航失败");
                        }
                    });
                });

                await Task.Delay(100);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                MessageBox.Show($"导航到 {item.DisplayName} 时发生错误：{ex.Message}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新菜单
        /// </summary>
        private async Task RefreshMenuAsync() {
            try {
                _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish("正在刷新菜单...");
                // 这里可以重新加载菜单项
                await Task.Delay(500); // 模拟加载
                _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish("菜单已刷新");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Refresh menu error: {ex.Message}");
                _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish($"刷新菜单失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 清除最近访问
        /// </summary>
        private void ClearRecentItems() {
            RecentItems.Clear();
            _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish("最近访问已清除");
        }

        /// <summary>
        /// 添加到快速访问
        /// </summary>
        private void AddToQuickAccess(NavigationItem item) {
            if (item == null || QuickAccessItems.Any(q => q.TargetView == item.TargetView))
                return;

            QuickAccessItems.Add(new NavigationItem(item.DisplayName, item.TargetView, item.Icon));
            _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish($"已添加 {item.DisplayName} 到快速访问");
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
                _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Publish($"已从快速访问移除 {item.DisplayName}");
            }
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

        #region Public Methods

        /// <summary>
        /// 加载导航菜单
        /// </summary>
        public async Task LoadNavigationAsync(IEnumerable<UserRole> roles) {
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
                NavigationCount = NavigationItems.Count;

                System.Diagnostics.Debug.WriteLine($"NavigationItems count after loading: {NavigationItems.Count}");

                await Task.CompletedTask;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"LoadNavigation error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset() {
            NavigationItems.Clear();
            QuickAccessItems.Clear();
            RecentItems.Clear();
            SelectedNavigationItem = null;
            NavigationCount = 0;
            HasNavigationItems = false;
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

        #endregion
    }
}