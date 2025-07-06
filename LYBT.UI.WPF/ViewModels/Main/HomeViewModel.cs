using System.Collections.ObjectModel;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Models;
using LYBT.Common.Extensions;
using Prism.Mvvm;
using Prism.Commands;
using Prism.Ioc;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 主内容区ViewModel，动态导航菜单
    /// </summary>
    public class HomeViewModel : BindableBase, INavigationAware {
        /// <summary>
        /// 属性 NavigationItems 的说明
        /// </summary>
        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

        private NavigationItem _selectedNavigationItem;
        public NavigationItem SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set => SetProperty(ref _selectedNavigationItem, value);
        }

        private readonly IRegionManager _regionManager;
        /// <summary>
        /// 属性 NavigateCommand 的说明
        /// </summary>
        public DelegateCommand<NavigationItem> NavigateCommand { get; }

        public HomeViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<NavigationItem>(Navigate);
            // 构造函数不再添加测试项
        }

        /// <summary>
        /// 方法 Navigate 的说明
        /// </summary>
        private void Navigate(NavigationItem item) {
            if (item != null) {
                SelectedNavigationItem = item;
                // 检查目标视图是否已注册（即是否存在对应的View类）
                var viewType = typeof(HomeViewModel).Assembly.GetType($"LYBT.UI.WPF.Views.{item.TargetView}");
                if (viewType == null) {
                    MessageBox.Show("该功能暂未开放或未实现。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"Navigating to: {item.TargetView}");
                _regionManager.RequestNavigate("ContentRegion", item.TargetView);
            }
        }

        // Prism导航时传递角色，根据角色加载菜单
        public void OnNavigatedTo(NavigationContext navigationContext) {
            System.Diagnostics.Debug.WriteLine($"HomeViewModel.OnNavigatedTo called");
            if (navigationContext.Parameters.TryGetValue("UserRoles", out IList<UserRole> roles)) {
                System.Diagnostics.Debug.WriteLine($"Found {roles.Count} user roles: {string.Join(", ", roles)}");
                LoadNavigation(roles);
            } else {
                System.Diagnostics.Debug.WriteLine("No UserRoles found in navigation parameters");
                NavigationItems.Clear();
                NavigationItems.Add(new NavigationItem("默认功能模块", "DefaultView"));
                System.Diagnostics.Debug.WriteLine("Added default navigation item for testing");
            }
        }

        // 动态加载导航菜单（可按角色自定义）
        private void LoadNavigation(IEnumerable<UserRole> roles) {
            System.Diagnostics.Debug.WriteLine($"LoadNavigation called with roles: {string.Join(", ", roles)}");
            NavigationItems.Clear();
            foreach (var role in roles.OrderBy(r => (int)r)) {
                var displayName = $"{role.GetDescription()}功能模块";
                switch (role) {
                    case UserRole.Admin:
                        NavigationItems.Add(new NavigationItem(displayName, "AdminView"));
                        break;
                    case UserRole.DiagnosingDoctor:
                        NavigationItems.Add(new NavigationItem(displayName, "DiagnosingDoctorView"));
                        break;
                    case UserRole.PharmacyStaff:
                        NavigationItems.Add(new NavigationItem(displayName, "PharmacyStaffView"));
                        break;
                    case UserRole.BillingStaff:
                        NavigationItems.Add(new NavigationItem(displayName, "BillingStaffView"));
                        break;
                    case UserRole.RegistrationStaff:
                        NavigationItems.Add(new NavigationItem(displayName, "RegistrationStaffView"));
                        break;
                    case UserRole.TreatmentDoctor:
                        NavigationItems.Add(new NavigationItem(displayName, "TreatmentDoctorView"));
                        break;
                    default:
                        break;
                }
            }
            System.Diagnostics.Debug.WriteLine($"NavigationItems count after loading: {NavigationItems.Count}");
        }

        /// <summary>
        /// 方法 IsNavigationTarget 的说明
        /// </summary>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        /// <summary>
        /// 方法 OnNavigatedFrom 的说明
        /// </summary>
        public void OnNavigatedFrom(NavigationContext navigationContext) { 
            System.Diagnostics.Debug.WriteLine("HomeViewModel.OnNavigatedFrom called");
        }
    }
}
