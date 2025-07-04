using System.Collections.ObjectModel;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Models;
using System.Linq;
using LYBT.Common.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 主内容区ViewModel，动态导航菜单
    /// </summary>
    public class HomeViewModel : BindableBase, INavigationAware {
        /// <summary>
        /// 属性 NavigationItems 的说明
        /// </summary>
        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

        private readonly IRegionManager _regionManager;
        /// <summary>
        /// 属性 NavigateCommand 的说明
        /// </summary>
        public DelegateCommand<NavigationItem> NavigateCommand { get; }

        public HomeViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<NavigationItem>(Navigate);
        }

        /// <summary>
        /// 方法 Navigate 的说明
        /// </summary>
        private void Navigate(NavigationItem item) {
            if (item != null)
                _regionManager.RequestNavigate("ContentRegion", item.TargetView);
        }

        // Prism导航时传递角色，根据角色加载菜单
        public void OnNavigatedTo(NavigationContext navigationContext) {
            if (navigationContext.Parameters.TryGetValue("UserRoles", out IList<UserRole> roles)) {
                LoadNavigation(roles);
            }
        }

        // 动态加载导航菜单（可按角色自定义）
        private void LoadNavigation(IEnumerable<UserRole> roles) {
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
        }

        /// <summary>
        /// 方法 IsNavigationTarget 的说明
        /// </summary>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        /// <summary>
        /// 方法 OnNavigatedFrom 的说明
        /// </summary>
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
