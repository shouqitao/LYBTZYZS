using LYBT.Module.Users.Dtos;
using LYBT.UI.WPF.Interfaces;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Profile {
    /// <summary>
    /// 用户资料视图模型
    /// </summary>
    public class UserProfileViewModel : BindableBase, INavigationAware {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        private UserDto? _user;
        public UserDto? User {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        public UserProfileViewModel(IUserService userService, IAuthService authService) {
            _userService = userService;
            _authService = authService;
        }

        public async Task LoadAsync() {
            try {
                User = await _userService.GetByIdAsync(_authService.UserId);
            } catch (Exception ex) {
                MessageBox.Show($"加载用户信息失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void OnNavigatedTo(NavigationContext navigationContext) {
            await LoadAsync();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
