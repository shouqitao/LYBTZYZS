using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 重置密码对话框 ViewModel
    /// TODO: 当前使用 Mock 实现，待后续集成真实服务
    /// </summary>
    public class ResetPasswordDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        private readonly IUserService _userService;
        private Guid _targetUserId;

        #region 属性

        private string _username = string.Empty;
        /// <summary>
        /// 目标用户名（只读显示）
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
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

        private bool _requirePasswordChange = true;
        /// <summary>
        /// 要求用户下次登录时修改密码
        /// </summary>
        public bool RequirePasswordChange
        {
            get => _requirePasswordChange;
            set => SetProperty(ref _requirePasswordChange, value);
        }

        private bool _sendNotification;
        /// <summary>
        /// 发送通知给用户
        /// </summary>
        public bool SendNotification
        {
            get => _sendNotification;
            set => SetProperty(ref _sendNotification, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _hasError;
        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        #endregion

        #region 命令

        public DelegateCommand GeneratePasswordCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region IDialogAware 实现

        public string Title => "重置密码";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                if (parameters.TryGetValue("UserId", out Guid userId))
                {
                    _targetUserId = userId;
                    _ = LoadUserInfoAsync(userId);
                }
                else
                {
                    Logger.LogError("ResetPasswordDialog 需要 UserId 参数");
                    SetError("缺少必要的参数");
                }

                // 可选参数
                if (parameters.TryGetValue("Username", out string username))
                {
                    Username = username;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开重置密码对话框时发生异常");
                SetError("对话框初始化失败");
            }
        }

        #endregion

        #region 构造函数

        public ResetPasswordDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IUserService userService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            GeneratePasswordCommand = new DelegateCommand(GenerateRandomPassword);

            ConfirmCommand = new DelegateCommand(async () => await ConfirmAsync(), CanConfirm)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);

            CancelCommand = new DelegateCommand(Cancel);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载用户信息
        /// </summary>
        private async Task LoadUserInfoAsync(Guid userId)
        {
            try
            {
                SetIsBusy(true, "正在加载用户信息...");

                var result = await _userService.GetByIdAsync(userId);
                if (result.IsSuccess && result.Data != null)
                {
                    Username = result.Data.UserName; // 注意：UserDto 属性名是 UserName，不是 Username
                }
                else
                {
                    SetError("无法加载用户信息");
                    Logger.LogWarning("加载用户信息失败: {ErrorMessage}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户信息时发生异常");
                SetError($"加载失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 生成随机密码
        /// </summary>
        private void GenerateRandomPassword()
        {
            try
            {
                const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
                const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                const string digitChars = "0123456789";
                const string specialChars = "!@#$%^&*()_+-=[]{}";

                var random = new Random();

                // 生成 12 位随机密码，确保包含各种类型的字符
                var password = new char[12];

                // 至少包含 2 个小写字母
                password[0] = lowerChars[random.Next(lowerChars.Length)];
                password[1] = lowerChars[random.Next(lowerChars.Length)];

                // 至少包含 2 个大写字母
                password[2] = upperChars[random.Next(upperChars.Length)];
                password[3] = upperChars[random.Next(upperChars.Length)];

                // 至少包含 2 个数字
                password[4] = digitChars[random.Next(digitChars.Length)];
                password[5] = digitChars[random.Next(digitChars.Length)];

                // 至少包含 2 个特殊字符
                password[6] = specialChars[random.Next(specialChars.Length)];
                password[7] = specialChars[random.Next(specialChars.Length)];

                // 剩余 4 位从所有字符中随机选择
                string allChars = lowerChars + upperChars + digitChars + specialChars;
                for (int i = 8; i < 12; i++)
                {
                    password[i] = allChars[random.Next(allChars.Length)];
                }

                // 打乱顺序
                var shuffled = password.OrderBy(x => random.Next()).ToArray();
                string generatedPassword = new string(shuffled);

                NewPassword = generatedPassword;
                ConfirmPassword = generatedPassword;

                Logger.LogInformation("已生成随机密码");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "生成随机密码时发生异常");
                SetError("生成密码失败");
            }
        }

        /// <summary>
        /// 验证密码输入
        /// </summary>
        private bool ValidatePasswords()
        {
            ClearError();

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                SetError("请输入新密码");
                return false;
            }

            if (NewPassword.Length < 8)
            {
                SetError("密码长度至少8个字符");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                SetError("请确认密码");
                return false;
            }

            if (NewPassword != ConfirmPassword)
            {
                SetError("两次输入的密码不一致");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否可以确认
        /// </summary>
        private bool CanConfirm()
        {
            return !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        /// <summary>
        /// 确认重置密码
        /// </summary>
        private async Task ConfirmAsync()
        {
            try
            {
                if (!ValidatePasswords())
                {
                    return;
                }

                if (_targetUserId == Guid.Empty)
                {
                    SetError("无效的用户ID");
                    return;
                }

                SetIsBusy(true, "正在重置密码...");

                // TODO: 当前 Client 端没有 ResetPassword 服务方法，暂时 Mock 成功
                // 真实实现需要调用服务端 API
                await Task.Delay(500); // 模拟网络延迟

                await ShowSuccessMessageAsync("密码重置成功");

                var dialogResult = new DialogResult(ButtonResult.OK);
                dialogResult.Parameters.Add("RequirePasswordChange", RequirePasswordChange);
                dialogResult.Parameters.Add("SendNotification", SendNotification);

                RequestClose?.Invoke(dialogResult);

                Logger.LogInformation(
                    "用户 {UserId} 密码重置成功 (要求修改密码: {RequireChange}, 发送通知: {SendNotification})",
                    _targetUserId,
                    RequirePasswordChange,
                    SendNotification);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "重置密码时发生异常");
                await ShowErrorMessageAsync($"重置密码失败: {ex.Message}");
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
        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        #endregion
    }
}
