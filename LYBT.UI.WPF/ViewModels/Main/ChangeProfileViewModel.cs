using Prism.Commands;
using Prism.Mvvm;
using Services = LYBT.UI.WPF.Services;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class ChangeProfileViewModel : BindableBase {
        private readonly Services.IUserService _userService;
        private readonly Services.IAuthService _authService;

        private string _originalRealName = string.Empty;
        public string OriginalRealName { get => _originalRealName; set => SetProperty(ref _originalRealName, value); }

        private string? _originalEmail;
        public string? OriginalEmail { get => _originalEmail; set => SetProperty(ref _originalEmail, value); }

        private string? _originalPhoneNumber;
        public string? OriginalPhoneNumber { get => _originalPhoneNumber; set => SetProperty(ref _originalPhoneNumber, value); }

        private string _realName = string.Empty;
        public string RealName { get => _realName; set => SetProperty(ref _realName, value); }

        private string? _email;
        public string? Email { get => _email; set => SetProperty(ref _email, value); }

        private string? _phoneNumber;
        public string? PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public ChangeProfileViewModel(Services.IUserService userService, Services.IAuthService authService) {
            _userService = userService;
            _authService = authService;
            SaveCommand = new DelegateCommand(async () => await ChangeAsync(), CanSave)
                .ObservesProperty(() => RealName);
            CancelCommand = new DelegateCommand(OnCancel);
            _ = LoadAsync();
        }

        public async Task LoadAsync() {
            var user = await _userService.GetByIdAsync(_authService.UserId);
            if (user != null) {
                OriginalRealName = user.RealName;
                OriginalEmail = user.Email;
                OriginalPhoneNumber = user.PhoneNumber;

                RealName = user.RealName;
                Email = user.Email;
                PhoneNumber = user.PhoneNumber;
            }
        }

        private bool CanSave() {
            return !string.IsNullOrWhiteSpace(RealName);
        }

        private async Task ChangeAsync() {
            ErrorMessage = string.Empty;
            var ok = await _userService.ChangeProfileAsync(_authService.UserId, RealName, Email, PhoneNumber);
            if (ok) {
                MessageBox.Show("信息已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                if (Application.Current.MainWindow.DataContext is MainWindowViewModel main) {
                    main.IsMainVisible = true;
                    main.IsFunctionVisible = false;
                }
                OriginalRealName = RealName;
                OriginalEmail = Email;
                OriginalPhoneNumber = PhoneNumber;
            } else {
                ErrorMessage = "修改失败";
            }
        }

        private void OnCancel() {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel main) {
                main.IsMainVisible = true;
                main.IsFunctionVisible = false;
            }
        }
    }
}
