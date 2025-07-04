using Prism.Commands;
using Prism.Mvvm;
using LYBT.UI.WPF.Services;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class ChangePasswordViewModel : BindableBase {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        private string _oldPassword = string.Empty;
        public string OldPassword { get => _oldPassword; set => SetProperty(ref _oldPassword, value); }

        private string _newPassword = string.Empty;
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public ChangePasswordViewModel(IUserService userService, IAuthService authService) {
            _userService = userService;
            _authService = authService;
            SaveCommand = new DelegateCommand(async () => await ChangeAsync(), CanSave)
                .ObservesProperty(() => OldPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        private bool CanSave() {
            return !string.IsNullOrWhiteSpace(OldPassword)
                && !string.IsNullOrWhiteSpace(NewPassword)
                && NewPassword == ConfirmPassword;
        }

        private async Task ChangeAsync() {
            ErrorMessage = string.Empty;
            var ok = await _userService.ChangePasswordAsync(_authService.UserId, OldPassword, NewPassword);
            if (ok) {
                MessageBox.Show("密码已修改，请重新登录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                if (Application.Current.MainWindow.DataContext is MainWindowViewModel main)
                {
                    main.LogoutCommand.Execute();
                }
            } else {
                ErrorMessage = "修改失败";
            }
        }

        private void OnCancel() {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel main)
            {
                main.IsMainVisible = true;
                main.IsFunctionVisible = false;
            }
        }
    }
}
