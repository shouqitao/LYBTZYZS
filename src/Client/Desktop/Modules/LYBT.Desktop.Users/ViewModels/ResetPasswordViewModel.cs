using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Events; // Issue #1928: 添加Events命名空间
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 重置密码视图模型 - Issue #1928 (Sprint 2)
    /// 管理员重置用户密码功能（Navigation模式）
    /// </summary>
    public class ResetPasswordViewModel : UnifiedViewModelBase
    {
        private readonly UserCommandHandler _commandHandler;
        private Guid _targetUserId;

        // Issue #1794: 密码生成字符集常量
        private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string DigitChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()_+-=[]{}";
        private const int PasswordLength = 12;

        #region 属性

        private UserDto? _user;
        /// <summary>
        /// 目标用户信息
        /// </summary>
        public UserDto? User
        {
            get => _user;
            set => SetProperty(ref _user, value);
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
                    ClearValidationError();
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
                    ClearValidationError();
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

        private string _validationError = string.Empty;
        /// <summary>
        /// 验证错误消息
        /// </summary>
        public string ValidationError
        {
            get => _validationError;
            set => SetProperty(ref _validationError, value);
        }

        private bool _hasValidationError;
        /// <summary>
        /// 是否有验证错误
        /// </summary>
        public bool HasValidationError
        {
            get => _hasValidationError;
            set => SetProperty(ref _hasValidationError, value);
        }

        #endregion

        #region 命令

        public DelegateCommand GeneratePasswordCommand { get; }
        public DelegateCommand ResetPasswordCommand { get; }
        public DelegateCommand GoBackCommand { get; }

        #endregion

        #region 构造函数

        public ResetPasswordViewModel(
            UserCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            GeneratePasswordCommand = new DelegateCommand(GenerateRandomPassword);

            ResetPasswordCommand = new DelegateCommand(async () => await ResetPasswordAsync(), CanResetPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);

            GoBackCommand = new DelegateCommand(ExecuteGoBack);
        }

        #endregion

        #region 导航处理

        /// <summary>
        /// 处理导航参数（同步）
        /// Issue #1240: 立即设置导航参数
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            if (parameters.ContainsKey("UserId"))
            {
                _targetUserId = parameters.GetValue<Guid>("UserId");
            }
        }

        /// <summary>
        /// 异步初始化数据
        /// Issue #1240: 使用 InitializeAsync 模式
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            if (_targetUserId != Guid.Empty)
            {
                await LoadUserAsync();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载用户信息
        /// </summary>
        private async Task LoadUserAsync()
        {
            if (_targetUserId == Guid.Empty)
            {
                Logger.LogWarning("LoadUserAsync: UserId 为空");
                return;
            }

            try
            {
                IsLoading = true;
                Logger.LogInformation("开始加载用户信息: UserId={UserId}", _targetUserId);

                var result = await _commandHandler.GetByIdAsync(_targetUserId);
                if (result.success && result.user != null)
                {
                    User = result.user;
                    PageTitle = $"重置密码 - {User.RealName}";
                    Logger.LogInformation("用户信息加载成功: {UserName}", User.UserName);
                }
                else
                {
                    Logger.LogWarning("未找到用户: UserId={UserId}, ErrorMessage={ErrorMessage}", _targetUserId, result.errorMessage);
                    ErrorMessage = result.errorMessage ?? "未找到该用户信息";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户信息失败: UserId={UserId}", _targetUserId);
                ErrorMessage = $"加载用户信息失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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
                SetValidationError("生成密码失败");
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
            ClearValidationError();

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                SetValidationError("请输入新密码");
                return false;
            }

            if (NewPassword.Length < 8)
            {
                SetValidationError("密码长度至少8个字符");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                SetValidationError("请确认密码");
                return false;
            }

            if (NewPassword != ConfirmPassword)
            {
                SetValidationError("两次输入的密码不一致");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否可以重置密码
        /// </summary>
        private bool CanResetPassword()
        {
            return !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   !IsLoading;
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        private async Task ResetPasswordAsync()
        {
            if (User == null)
            {
                Logger.LogWarning("无法重置密码：用户为空");
                return;
            }

            try
            {
                if (!ValidatePasswords())
                {
                    return;
                }

                if (_targetUserId == Guid.Empty)
                {
                    SetValidationError("无效的用户ID");
                    return;
                }

                IsLoading = true;
                Logger.LogInformation("开始重置密码: UserId={UserId}", _targetUserId);

                // 调用重置密码服务
                var (success, errorMessage, response) = await _commandHandler.ResetPasswordAsync(
                    _targetUserId,
                    NewPassword);

                if (!success || response == null)
                {
                    ErrorMessage = $"密码重置失败: {errorMessage}";
                    Logger.LogWarning("密码重置失败: {ErrorMessage}", errorMessage);
                    return;
                }

                // 显示成功消息（包含临时密码）
                await ShowSuccessMessageAsync(
                    $"密码重置成功！\n\n" +
                    $"用户: {User.UserName}\n" +
                    $"新密码: {response.TemporaryPassword}\n\n" +
                    $"请妄善保管并告知用户。");

                // Issue #1928: 发布事件通知订阅者
                EventAggregator.GetEvent<UserPasswordResetEvent>().Publish(User);

                Logger.LogInformation(
                    "用户 {UserId} 密码重置成功 (要求修改密码: {RequireChange}, 发送通知: {SendNotification})",
                    _targetUserId,
                    RequirePasswordChange,
                    SendNotification);

                // 返回上一页
                NavigateBack("ContentRegion");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "重置密码时发生异常: UserId={UserId}", _targetUserId);
                ErrorMessage = $"重置密码失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 返回上一页
        /// </summary>
        private void ExecuteGoBack()
        {
            Logger.LogInformation("取消重置密码，返回上一页");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 设置验证错误
        /// </summary>
        private void SetValidationError(string message)
        {
            ValidationError = message;
            HasValidationError = true;
        }
        /// <summary>
        /// 清除验证错误
        /// </summary>
        private void ClearValidationError()
        {
            ValidationError = string.Empty;
            HasValidationError = false;
        }

        #endregion
    }
}
