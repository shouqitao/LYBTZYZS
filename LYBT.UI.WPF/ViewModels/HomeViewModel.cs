using LYBT.Common.Enums.Users;
using System.Collections.ObjectModel;
using LYBT.UI.WPF.Models;

namespace LYBT.UI.WPF.ViewModels {

    /// <summary>
    /// HomeView 对应的视图模型
    /// </summary>
    public class HomeViewModel : BindableBase, INavigationAware {

        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

        public DelegateCommand<NavigationItem> NavigateCommand { get; }
        private readonly IRegionManager _regionManager;

        public HomeViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<NavigationItem>(Navigate);
        }

        private void Navigate(NavigationItem? item) {
            if (item != null) {
                _regionManager.RequestNavigate("HomeContentRegion", item.TargetView);
            }
        }

        private static readonly List<UserRole> _roleOrder = new() {
            UserRole.RegistrationStaff,
            UserRole.DiagnosingDoctor,
            UserRole.Admin,            // 视作收费/管理角色
            UserRole.PharmacyStaff,
            UserRole.TreatmentDoctor
        };

        private void LoadNavigation(IEnumerable<UserRole> roles) {
            NavigationItems.Clear();
            var orderedRoles = roles.Distinct()
                .OrderBy(r => {
                    var index = _roleOrder.IndexOf(r);
                    return index >= 0 ? index : int.MaxValue;
                });

            NavigationItem? first = null;
            foreach (var role in orderedRoles) {
                NavigationItem? item = role switch {
                    UserRole.Admin => new NavigationItem("管理员面板", "AdminView"),
                    UserRole.DiagnosingDoctor => new NavigationItem("医生面板", "DiagnosingDoctorView"),
                    UserRole.TreatmentDoctor => new NavigationItem("治疗面板", "TreatmentDoctorView"),
                    UserRole.PharmacyStaff => new NavigationItem("药房面板", "PharmacyStaffView"),
                    UserRole.RegistrationStaff => new NavigationItem("挂号面板", "RegistrationStaffView"),
                    _ => null
                };

                if (item != null) {
                    NavigationItems.Add(item);
                    first ??= item;
                }
            }

            if (first != null) {
                Navigate(first);
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext) {
            if (navigationContext.Parameters.TryGetValue("UserRoles", out IList<UserRole> roles)) {
                LoadNavigation(roles);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}