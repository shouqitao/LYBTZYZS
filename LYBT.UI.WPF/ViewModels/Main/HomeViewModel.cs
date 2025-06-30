using System.Collections.ObjectModel;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Models;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 主内容区ViewModel，动态导航菜单
    /// </summary>
    public class HomeViewModel : BindableBase, INavigationAware {
        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

        private readonly IRegionManager _regionManager;
        public DelegateCommand<NavigationItem> NavigateCommand { get; }

        public HomeViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<NavigationItem>(Navigate);
        }

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
            foreach (var role in roles) {
                switch (role) {
                    case UserRole.Admin:
                        NavigationItems.Add(new NavigationItem("管理员面板", "AdminView"));
                        break;
                    case UserRole.DiagnosingDoctor:
                        NavigationItems.Add(new NavigationItem("医生面板", "DiagnosingDoctorView"));
                        break;
                    case UserRole.PharmacyStaff:
                        NavigationItems.Add(new NavigationItem("药房面板", "PharmacyStaffView"));
                        break;
                    case UserRole.BillingStaff:
                        NavigationItems.Add(new NavigationItem("收费面板", "BillingStaffView"));
                        break;
                    case UserRole.RegistrationStaff:
                        NavigationItems.Add(new NavigationItem("挂号面板", "RegistrationStaffView"));
                        break;
                    case UserRole.TreatmentDoctor:
                        NavigationItems.Add(new NavigationItem("治疗面板", "TreatmentDoctorView"));
                        break;
                    default:
                        break;
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
