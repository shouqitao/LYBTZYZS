using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 修改密码视图模型 - Issue #1929 (Sprint 3)
    /// 用户修改自己的密码（Navigation模式）
    /// </summary>
    public class ChangePasswordViewModel : UnifiedViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserRepository _userRepository;
        private readonly ISessionManager _sessionManager;
        private Guid _currentUserId;

        #region 属性

        private string _oldPassword = string.Empty;
        /// <summary>
        /// 旧密码
        /// </summary>
        public string OldPassword
        {
            get => _oldPassword;
            set => SetProperty(ref _oldPassword, value);
        }

        private string _newPassword = string.Empty;
        /// <summary>
        /// 新密码
        /// </summary>
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        private string _confirmPassword = string.Empty;
        /// <summary>
        /// 确认密码
        /// </summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private string _userName = string.Empty;
        /// <summary>
        /// 用户名（显示用）
        /// </summary>
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string? _validationError;
        /// <summary>
        /// 验证错误信息
        /// </summary>
        public string? ValidationError
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

        public DelegateCommand ChangePasswordCommand { get; }
        public DelegateCommand GoBackCommand { get; }

        #endregion

        #region 构造函数

        public ChangePasswordViewModel(
            IAuthenticationService authService,
            IUserRepository userRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager sessionManager,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

            PageTitle = "修改密码";

            // 初始化命令
            ChangePasswordCommand = new DelegateCommand(async () => await ChangePasswordAsync(), CanChangePassword)
                .ObservesProperty(() => OldPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);

            GoBackCommand = new DelegateCommand(ExecuteGoBack);
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 异步初始化数据
        /// Issue #1240: 使用 InitializeAsync 模式
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            try
            {
                // 从SessionManager获取当前用户
                var currentUser = _sessionManager?.CurrentUser;
                if (currentUser == null)
                {
                    Logger.LogError("SessionManager.CurrentUser 为空，无法修改密码");
                    ErrorMessage = "无法获取当前用户信息，请重新登录";
                    return;
                }

                UserName = currentUser.UserName;
                _currentUserId = currentUser.Id;

                Logger.LogInformation("ChangePasswordView 加载，用户: {UserName} (ID: {UserId})",
                    UserName, _currentUserId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载修改密码页面时发生异常");
                ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载", ex);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 返回上一页
        /// </summary>
        private void ExecuteGoBack()
        {
            Logger.LogInformation("取消修改密码，返回上一页");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 是否可以修改密码
        /// </summary>
        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(OldPassword)
                && !string.IsNullOrWhiteSpace(NewPassword)
                && !string.IsNullOrWhiteSpace(ConfirmPassword)
                && !IsLoading;
        }

        /// <summary>
        /// 验证密码输入
        /// </summary>
        private bool ValidatePassword()
        {
            // 验证新密码与确认密码一致
            if (NewPassword != ConfirmPassword)
            {
                ValidationError = "新密码与确认密码不一致";
                HasValidationError = true;
                return false;
            }

            // 验证密码长度
            if (NewPassword.Length < 6)
            {
                ValidationError = "密码长度至少6个字符";
                HasValidationError = true;
                return false;
            }

            // 验证新密码不能与旧密码相同
            if (NewPassword == OldPassword)
            {
                ValidationError = "新密码不能与旧密码相同";
                HasValidationError = true;
                return false;
            }

            // 清除验证错误
            ValidationError = null;
            HasValidationError = false;
            return true;
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        private async Task ChangePasswordAsync()
        {
            try
            {
                // 验证密码
                if (!ValidatePassword())
                {
                    return;
                }

                IsLoading = true;
                StatusMessage = "正在修改密码...";

                // 调用API修改密码
                var request = new ChangePasswordRequest
                {
                    OldPassword = OldPassword,
                    NewPassword = NewPassword
                };

                var result = await _userRepository.ChangePasswordAsync(_currentUserId, request);

                if (result.IsSuccess)
                {
                    Logger.LogInformation("用户 {UserName} 密码修改成功，准备自动logout", UserName);

                    // ⭐ Issue #1906修复 + 竞态条件修复：确保Token完全清除后再导航

                    // 1. 自动logout（清除Server端和Client端的所有Token）
                    await _authService.LogoutAsync();
                    Logger.LogInformation("用户 {UserName} Token已清除", UserName);

                    // 2. 额外延迟，确保Token清除操作完全完成（避免竞态条件）
                    await Task.Delay(100);

                    // 3. 导航返回（此时Token已确保被清除）
                    NavigateBack("ContentRegion");

                    // 4. 发布密码修改事件（触发导航到登录界面）
                    EventAggregator.GetEvent<PasswordChangedEvent>().Publish();

                    Logger.LogInformation("用户 {UserName} 已返回并导航到登录界面", UserName);

                    // 5. 稍微延迟，确保导航完成
                    await Task.Delay(200);

                    // 6. 显示成功消息（此时只有登录界面和MessageBox）
                    await ShowSuccessMessageAsync("密码修改成功！\n\n请使用新密码重新登录。");
                }
                else
                {
                    Logger.LogWarning("用户 {UserName} 密码修改失败: {Message}", UserName, result.Message);
                    ValidationError = result.Message ?? "密码修改失败";
                    HasValidationError = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "修改密码时发生异常");
                ValidationError = ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex);
                HasValidationError = true;
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        #endregion
    }
}
