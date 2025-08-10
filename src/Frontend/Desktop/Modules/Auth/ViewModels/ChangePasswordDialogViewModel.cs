using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.WPF.Client.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.Auth.ViewModels
{
    /// <summary>
    /// 修改密码对话框视图模型
    /// </summary>
    public class ChangePasswordDialogViewModel : BindableBase
    {
        private readonly IAuthApiService _authApiService;
        private readonly IAuthenticationService _authenticationService;

        #region Properties

        private string _dialogTitle = "修改密码";
        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        private string _currentPassword = string.Empty;
        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
        }

        private string _newPassword = string.Empty;
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private string _passwordStrength = string.Empty;
        public string PasswordStrength
        {
            get => _passwordStrength;
            set => SetProperty(ref _passwordStrength, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand<object> CurrentPasswordChangedCommand { get; }
        public DelegateCommand<object> NewPasswordChangedCommand { get; }
        public DelegateCommand<object> ConfirmPasswordChangedCommand { get; }

        #endregion

        #region Callbacks

        /// <summary>
        /// 保存完成回调
        /// </summary>
        public Action<bool>? SaveCompleteCallback { get; set; }

        #endregion

        #region Constructor

        public ChangePasswordDialogViewModel(
            IAuthApiService authApiService,
            IAuthenticationService authenticationService)
        {
            _authApiService = authApiService ?? throw new ArgumentNullException(nameof(authApiService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave)
                .ObservesProperty(() => CurrentPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword)
                .ObservesProperty(() => IsLoading);

            CancelCommand = new DelegateCommand(ExecuteCancel);
            
            CurrentPasswordChangedCommand = new DelegateCommand<object>(ExecuteCurrentPasswordChanged);
            NewPasswordChangedCommand = new DelegateCommand<object>(ExecuteNewPasswordChanged);
            ConfirmPasswordChangedCommand = new DelegateCommand<object>(ExecuteConfirmPasswordChanged);
        }

        #endregion

        #region Private Methods

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   !IsLoading;
        }

        private async Task ExecuteSaveAsync()
        {
            ErrorMessage = string.Empty;

            // 验证密码
            if (!ValidatePasswords())
            {
                return;
            }

            try
            {
                IsLoading = true;

                var changePasswordRequest = new ChangePasswordRequest
                {
                    CurrentPassword = CurrentPassword,
                    NewPassword = NewPassword
                };

                var response = await _authApiService.ChangePasswordAsync(changePasswordRequest);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("密码修改成功！请使用新密码重新登录。", "成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // 调用回调
                    SaveCompleteCallback?.Invoke(true);

                    // 可选：注销当前用户，强制重新登录
                    await _authenticationService.LogoutAsync();
                }
                else
                {
                    ErrorMessage = "密码修改失败，请重试";
                    SaveCompleteCallback?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"修改密码时发生错误：{ex.Message}";
                SaveCompleteCallback?.Invoke(false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancel()
        {
            SaveCompleteCallback?.Invoke(false);
        }

        private void ExecuteCurrentPasswordChanged(object passwordBox)
        {
            if (passwordBox is System.Windows.Controls.PasswordBox pb)
            {
                CurrentPassword = pb.Password;
            }
        }

        private void ExecuteNewPasswordChanged(object passwordBox)
        {
            if (passwordBox is System.Windows.Controls.PasswordBox pb)
            {
                NewPassword = pb.Password;
                UpdatePasswordStrength();
            }
        }

        private void ExecuteConfirmPasswordChanged(object passwordBox)
        {
            if (passwordBox is System.Windows.Controls.PasswordBox pb)
            {
                ConfirmPassword = pb.Password;
            }
        }

        private bool ValidatePasswords()
        {
            // 清空错误消息
            ErrorMessage = string.Empty;

            // 验证新密码和确认密码是否一致
            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "新密码和确认密码不一致";
                return false;
            }

            // 验证新密码和当前密码是否相同
            if (CurrentPassword == NewPassword)
            {
                ErrorMessage = "新密码不能与当前密码相同";
                return false;
            }

            // 验证密码强度
            if (NewPassword.Length < 6)
            {
                ErrorMessage = "密码长度至少为6个字符";
                return false;
            }

            // 验证密码复杂度（可选）
            if (!IsPasswordComplex(NewPassword))
            {
                ErrorMessage = "密码必须包含大小写字母、数字和特殊字符";
                return false;
            }

            return true;
        }

        private bool IsPasswordComplex(string password)
        {
            // 简单的密码复杂度验证
            // 可以根据实际需求调整规则
            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;
            bool hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                if (char.IsLower(c)) hasLower = true;
                if (char.IsDigit(c)) hasDigit = true;
                if (!char.IsLetterOrDigit(c)) hasSpecial = true;
            }

            // 至少包含三种类型的字符
            int complexity = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + 
                           (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);
            
            return complexity >= 3;
        }

        private void UpdatePasswordStrength()
        {
            if (string.IsNullOrEmpty(NewPassword))
            {
                PasswordStrength = string.Empty;
                return;
            }

            if (NewPassword.Length < 6)
            {
                PasswordStrength = "弱";
            }
            else if (NewPassword.Length < 10 && IsPasswordComplex(NewPassword))
            {
                PasswordStrength = "中";
            }
            else if (NewPassword.Length >= 10 && IsPasswordComplex(NewPassword))
            {
                PasswordStrength = "强";
            }
            else
            {
                PasswordStrength = "弱";
            }
        }

        #endregion
    }
}