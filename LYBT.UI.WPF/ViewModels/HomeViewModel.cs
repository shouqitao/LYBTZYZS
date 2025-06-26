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

        private void LoadNavigation(UserRole role) {
            NavigationItems.Clear();
            switch (role) {
                case UserRole.Admin:
                    NavigationItems.Add(new NavigationItem("管理员面板", "AdminView"));
                    break;
                case UserRole.DiagnosingDoctor:
                    NavigationItems.Add(new NavigationItem("医生面板", "DiagnosingDoctorView"));
                    break;
                case UserRole.TreatmentDoctor:
                    NavigationItems.Add(new NavigationItem("治疗面板", "TreatmentDoctorView"));
                    break;
                case UserRole.PharmacyStaff:
                    NavigationItems.Add(new NavigationItem("药房面板", "PharmacyStaffView"));
                    break;
                case UserRole.RegistrationStaff:
                    NavigationItems.Add(new NavigationItem("挂号面板", "RegistrationStaffView"));
                    break;
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext) {
            if (navigationContext.Parameters.TryGetValue("UserRole", out UserRole role)) {
                LoadNavigation(role);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}