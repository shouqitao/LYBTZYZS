using System.Text.RegularExpressions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 修改密码对话框 ViewModel
    /// TODO: 当前使用简化的 AuthService Mock 实现，待后续集成真实服务
    /// </summary>
    public class ChangePasswordDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        private readonly AuthService _authService;

        #region 属性

        private string _currentPassword = string.Empty;
        /// <summary>
        /// 当前密码
        /// </summary>
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                if (SetProperty(ref _currentPassword, value))
                {
                    ClearError();
                }
            }
        }

        private string _newPassword = string.Empty;
        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                if (SetProperty(ref _newPassword, value))
                {
                    CalculatePasswordStrength();
                    ClearError();
                }
            }
        }

        private string _confirmPassword = string.Empty;
        /// <summary>
        /// 确认密码
        /// </summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    ClearError();
                }
            }
        }

        private int _passwordStrength;
        /// <summary>
        /// 密码强度 (0-3: 无/弱/中/强)
        /// </summary>
        public int PasswordStrength
        {
            get => _passwordStrength;
            set => SetProperty(ref _passwordStrength, value);
        }

        private string _passwordStrengthText = string.Empty;
        /// <summary>
        /// 密码强度文本
        /// </summary>
        public string PasswordStrengthText
        {
            get => _passwordStrengthText;
            set => SetProperty(ref _passwordStrengthText, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>
        /// 错误消息
        /// </summary>
        public new string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _hasError;
        /// <summary>
        /// 是否有错误
        /// </summary>
        public new bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        #endregion

        #region 命令

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region IDialogAware 实现

        public string Title => "修改密码";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 修改密码对话框不需要参数
            ClearForm();
        }

        #endregion

        #region 构造函数

        public ChangePasswordDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            AuthService authService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            ConfirmCommand = new DelegateCommand(async () => await ConfirmAsync(), CanConfirm)
                .ObservesProperty(() => CurrentPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);

            CancelCommand = new DelegateCommand(Cancel);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算密码强度
        /// </summary>
        private void CalculatePasswordStrength()
        {
            if (string.IsNullOrEmpty(NewPassword))
            {
                PasswordStrength = 0;
                PasswordStrengthText = string.Empty;
                return;
            }

            int strength = 0;
            string password = NewPassword;

            // 长度检查
            if (password.Length >= 8) strength++;
            if (password.Length >= 12) strength++;

            // 复杂度检查
            if (Regex.IsMatch(password, @"[a-z]") && Regex.IsMatch(password, @"[A-Z]")) strength++;
            if (Regex.IsMatch(password, @"\d")) strength++;
            if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]")) strength++;

            // 综合评分（最高 3 分）
            PasswordStrength = Math.Min(strength / 2, 3);

            PasswordStrengthText = PasswordStrength switch
            {
                1 => "密码强度：弱",
                2 => "密码强度：中",
                3 => "密码强度：强",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 验证密码输入
        /// </summary>
        private bool ValidatePasswords()
        {
            ClearError();

            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                SetError("请输入当前密码");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                SetError("请输入新密码");
                return false;
            }

            if (NewPassword.Length < 8)
            {
                SetError("新密码长度至少8个字符");
                return false;
            }

            if (!Regex.IsMatch(NewPassword, @"[a-z]") || !Regex.IsMatch(NewPassword, @"[A-Z]"))
            {
                SetError("新密码必须包含大小写字母");
                return false;
            }

            if (!Regex.IsMatch(NewPassword, @"\d"))
            {
                SetError("新密码必须包含数字");
                return false;
            }

            if (!Regex.IsMatch(NewPassword, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
            {
                SetError("新密码必须包含特殊字符");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                SetError("请确认新密码");
                return false;
            }

            if (NewPassword != ConfirmPassword)
            {
                SetError("两次输入的新密码不一致");
                return false;
            }

            if (NewPassword == CurrentPassword)
            {
                SetError("新密码不能与当前密码相同");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否可以确认
        /// </summary>
        private bool CanConfirm()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        /// <summary>
        /// 确认修改密码
        /// </summary>
        private async Task ConfirmAsync()
        {
            try
            {
                if (!ValidatePasswords())
                {
                    return;
                }

                SetIsBusy(true, "正在修改密码...");

                // TODO: 当前使用 Mock AuthService，只接受 2 个参数
                // 真实实现需要调用服务端 API
                var result = await _authService.ChangePasswordAsync(CurrentPassword, NewPassword);

                if (result)
                {
                    await ShowSuccessMessageAsync("密码修改成功");
                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
                    Logger.LogInformation("密码修改成功");
                }
                else
                {
                    SetError("密码修改失败：当前密码不正确");
                    Logger.LogWarning("密码修改失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "修改密码时发生异常");
                await ShowErrorMessageAsync($"修改密码失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        /// <summary>
        /// 设置错误
        /// </summary>
        private void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        private new void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ClearForm()
        {
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            PasswordStrength = 0;
            PasswordStrengthText = string.Empty;
            ClearError();
        }

        #endregion
    }
}
