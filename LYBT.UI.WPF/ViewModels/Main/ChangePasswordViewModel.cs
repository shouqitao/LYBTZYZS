using Prism.Mvvm;
using System;
using LYBT.UI.WPF.Services;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class ChangePasswordViewModel : BindableBase {
        private string _oldPassword = string.Empty;
        public string OldPassword { get => _oldPassword; set => SetProperty(ref _oldPassword, value); }

        private string _newPassword = string.Empty;
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
    }
}
