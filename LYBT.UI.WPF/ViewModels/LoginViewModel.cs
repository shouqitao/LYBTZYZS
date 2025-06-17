using Prism.Commands;
using Prism.Mvvm;
using System.Windows.Input;

namespace LYBT.UI.WPF.ViewModels {
    /// <summary>
    /// Login page view model
    /// </summary>
    public class LoginViewModel : BindableBase {
        private string _username = string.Empty;
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel() {
            LoginCommand = new DelegateCommand(OnLogin);
        }

        private void OnLogin() {
            // TODO: implement authentication logic
        }
    }
}
