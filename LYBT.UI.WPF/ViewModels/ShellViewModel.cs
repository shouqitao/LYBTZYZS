using LYBT.UI.WPF.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using LYBT.Common.Enums.Users;

namespace LYBT.UI.WPF.ViewModels {
    /// <summary>
    /// View model for the main shell window
    /// </summary>
    public class ShellViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private readonly Services.TokenService _tokenService;

        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
        public ICommand NavigateCommand { get; }

        public ShellViewModel(IRegionManager regionManager, Services.TokenService tokenService) {
            _regionManager = regionManager;
            _tokenService = tokenService;
            NavigateCommand = new DelegateCommand<string?>(Navigate);

            BuildNavigation();
            if (NavigationItems.Count > 0) {
                _regionManager.RequestNavigate("MainRegion", NavigationItems[0].ViewName);
            }
        }

        private void BuildNavigation() {
            var user = _tokenService.CurrentUser;
            if (user == null)
                return;

            var roles = user.Roles.Count > 0 ? user.Roles : new List<UserRole> { user.Role };
            foreach (var role in roles) {
                switch (role) {
                    case UserRole.Admin:
                        NavigationItems.Add(new NavigationItem("管理员主页", nameof(AdminView)));
                        break;
                    case UserRole.DiagnosingDoctor:
                        NavigationItems.Add(new NavigationItem("看诊医生", nameof(DiagnosingDoctorView)));
                        break;
                    case UserRole.TreatmentDoctor:
                        NavigationItems.Add(new NavigationItem("诊疗室医生", nameof(TreatmentDoctorView)));
                        break;
                    case UserRole.PharmacyStaff:
                        NavigationItems.Add(new NavigationItem("药房", nameof(PharmacyStaffView)));
                        break;
                    case UserRole.RegistrationStaff:
                        NavigationItems.Add(new NavigationItem("挂号", nameof(RegistrationStaffView)));
                        break;
                }
            }
        }

        private void Navigate(string? view) {
            if (!string.IsNullOrWhiteSpace(view)) {
                _regionManager.RequestNavigate("MainRegion", view);
            }
        }
    }
}
