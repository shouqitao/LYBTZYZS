using LYBT.Module.Users.Dtos;
using LYBT.UI.WPF.Services;
using System.Collections.ObjectModel;

namespace LYBT.UI.WPF.ViewModels {
    public class AdminViewModel : BindableBase, INavigationAware {
        private readonly IUserManagementService _service;

        public ObservableCollection<UserDto> Users { get; } = new();

        public DelegateCommand AddUserCommand { get; }
        public DelegateCommand<UserDto?> EditUserCommand { get; }
        public DelegateCommand<UserDto?> ToggleUserStatusCommand { get; }

        public AdminViewModel(IUserManagementService service) {
            _service = service;
            AddUserCommand = new DelegateCommand(AddUser);
            EditUserCommand = new DelegateCommand<UserDto?>(EditUser);
            ToggleUserStatusCommand = new DelegateCommand<UserDto?>(ToggleUserStatus);
        }

        private async void LoadUsers() {
            var (list, _) = await _service.SearchAsync(new UserQueryDto { Page = 1, PageSize = 50 });
            Users.Clear();
            foreach (var u in list) {
                Users.Add(u);
            }
        }

        private async void AddUser() {
            var roles = await _service.GetRolesAsync();
            var dlg = new Views.UserEditWindow(roles);
            if (dlg.ShowDialog() == true && dlg.CreatedUser != null) {
                if (await _service.AddAsync(dlg.CreatedUser)) {
                    LoadUsers();
                }
            }
        }

        private async void EditUser(UserDto? user) {
            if (user == null)
                return;
            var roles = await _service.GetRolesAsync();
            var dlg = new Views.UserEditWindow(roles, user);
            if (dlg.ShowDialog() == true && dlg.EditedUser != null) {
                if (await _service.UpdateAsync(dlg.EditedUser)) {
                    LoadUsers();
                }
            }
        }

        private async void ToggleUserStatus(UserDto? user) {
            if (user == null)
                return;
            bool ok = user.IsActive ? await _service.DisableAsync(user.Id) : await _service.EnableAsync(user.Id);
            if (ok) {
                LoadUsers();
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext) {
            LoadUsers();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
