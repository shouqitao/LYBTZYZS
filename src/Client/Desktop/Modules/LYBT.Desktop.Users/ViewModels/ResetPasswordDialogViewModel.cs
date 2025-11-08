using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.ViewModels.Components; // Issue #1785: 添加Component命名空间
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
        // Issue #1785: 使用CommandHandler替代直接Repository访问
        private readonly UserCommandHandler _commandHandler;
        private Guid _targetUserId;

        // Issue #1794: 密码生成字符集常量
        private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string DigitChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()_+-=[]{}";
        private const int PasswordLength = 12;

        #region 属性

        private string _username = string.Empty;
        /// <summary>
        /// 目标用户名（只读显示）
        /// </summary>
        public string UserName
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
                if (parameters.TryGetValue("UserName", out string username))
                {
                    UserName = username;
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
            UserCommandHandler commandHandler, // Issue #1785: 注入CommandHandler
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1785: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

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

                // Issue #1785: 使用CommandHandler查询
                var result = await _commandHandler.GetByIdAsync(userId);
                if (result.success && result.user != null)
                {
                    UserName = result.user.UserName; // 注意：UserDto 属性名是 UserName
                }
                else
                {
                    SetError(result.errorMessage ?? "无法加载用户信息");
                    Logger.LogWarning("加载用户信息失败：{ErrorMessage}", result.errorMessage);
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
                string generatedPassword = GeneratePasswordCore();
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
        /// 核心密码生成逻辑
        /// Issue #1794: 提取密码生成逻辑
        /// </summary>
        private static string GeneratePasswordCore()
        {
            var random = new Random();
            var password = new char[PasswordLength];

            // 确保包含各种类型的字符（每种至少2个）
            FillPasswordCharacters(password, random, LowerChars, 0, 2);
            FillPasswordCharacters(password, random, UpperChars, 2, 2);
            FillPasswordCharacters(password, random, DigitChars, 4, 2);
            FillPasswordCharacters(password, random, SpecialChars, 6, 2);

            // 剩余位置从所有字符中随机选择
            string allChars = LowerChars + UpperChars + DigitChars + SpecialChars;
            for (int i = 8; i < PasswordLength; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // 打乱顺序
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        /// <summary>
        /// 填充密码字符
        /// Issue #1794: 提取字符填充逻辑
        /// </summary>
        private static void FillPasswordCharacters(char[] password, Random random, string charSet, int startIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                password[startIndex + i] = charSet[random.Next(charSet.Length)];
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

                // Issue #1911: 调用真实的重置密码服务
                var (success, errorMessage, response) = await _commandHandler.ResetPasswordAsync(
                    _targetUserId, 
                    NewPassword);

                if (!success || response == null)
                {
                    await ShowErrorMessageAsync($"密码重置失败: {errorMessage}");
                    return;
                }

                await ShowSuccessMessageAsync(
                    $"密码重置成功！\n\n" +
                    $"用户: {UserName}\n" +
                    $"新密码: {response.TemporaryPassword}\n\n" +
                    $"请妥善保管并告知用户。");

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
        private new void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        #endregion
    }
}
