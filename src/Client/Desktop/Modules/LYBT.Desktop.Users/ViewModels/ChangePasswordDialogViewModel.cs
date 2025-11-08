using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 修改密码对话框 ViewModel
    /// Issue #1887-1892: 独立的密码修改对话框，支持 sysadmin 和普通用户
    /// </summary>
    public class ChangePasswordDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserRepository _userRepository;
        private readonly ISessionManager _sessionManager;
        private Guid _currentUserId;
        private bool _isSysAdmin;

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

        #endregion

        #region IDialogAware

        public string Title => _isSysAdmin ? "修改管理员密码" : "修改密码";

        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 命令

        public DelegateCommand ChangePasswordCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public ChangePasswordDialogViewModel(
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

            // 初始化命令
            ChangePasswordCommand = new DelegateCommand(async () => await ChangePasswordAsync(), CanChangePassword)
                .ObservesProperty(() => OldPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);

            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel)));
        }

        #endregion

        #region IDialogAware 实现

        public bool CanCloseDialog() => !IsBusy;

        public void OnDialogClosed()
        {
            // 清理资源
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                // 判断是否为 sysadmin 模式
                _isSysAdmin = parameters.GetValue<bool>("IsSysAdmin");

                if (_isSysAdmin)
                {
                    // sysadmin 模式：从配置读取用户名
                    UserName = "sysadmin";
                    _currentUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                }
                else
                {
                    // 普通用户模式：从会话获取当前用户
                    var currentUser = _sessionManager?.CurrentUser;
                    if (currentUser == null)
                    {
                        Logger.LogError("SessionManager.CurrentUser 为空");
                        return;
                    }

                    UserName = currentUser.UserName;
                    _currentUserId = currentUser.Id;
                }

                Logger.LogInformation("ChangePasswordDialog 打开，模式: {Mode}, 用户: {UserName}",
                    _isSysAdmin ? "sysadmin" : "普通用户", UserName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开修改密码对话框时发生异常");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 是否可以修改密码
        /// </summary>
        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(OldPassword)
                && !string.IsNullOrWhiteSpace(NewPassword)
                && !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        private async Task ChangePasswordAsync()
        {
            try
            {

                // 验证密码
                if (NewPassword != ConfirmPassword)
                {
                    return;
                }

                if (NewPassword.Length < 6)
                {
                    return;
                }

                if (NewPassword == OldPassword)
                {
                    return;
                }

                SetIsBusy(true, "正在修改密码...");

                if (_isSysAdmin)
                {
                    // sysadmin 模式：调用 ChangeSysAdminPasswordAsync
                    var request = new ChangeSysAdminPassword
                    {
                        OldPassword = OldPassword,
                        NewPassword = NewPassword
                    };

                    var result = await _authService.ChangeSysAdminPasswordAsync(request);

                    if (result.IsSuccess)
                    {
                        Logger.LogInformation("sysadmin 密码修改成功，准备自动logout");

                        // ⭐ Issue #1906修复：调整执行顺序，先完成所有操作，最后关闭对话框

                        // 1. 自动logout（清除Server端和Client端的所有Token）
                        await _authService.LogoutAsync();

                        // 2. 先关闭对话框（避免对话框变空白）
                        SetIsBusy(false);
                        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));

                        // 3. 导航到登录界面
                        EventAggregator.GetEvent<PasswordChangedEvent>().Publish();

                        Logger.LogInformation("sysadmin 已关闭对话框并导航到登录界面");

                        // 4. 稍微延迟，确保对话框关闭和UI更新完成
                        await Task.Delay(200);

                        // 5. 显示成功消息（此时只有登录界面和MessageBox）
                        await ShowSuccessMessageAsync("密码修改成功！\n\n请使用新密码重新登录。");
                    }
                    else
                    {
                        Logger.LogWarning("sysadmin 密码修改失败: {Message}", result.Message);
                    }
                }
                else
                {
                    // 普通用户模式：调用 UserRepository.ChangePasswordAsync
                    var request = new ChangePasswordRequest
                    {
                        OldPassword = OldPassword,
                        NewPassword = NewPassword
                    };

                    var result = await _userRepository.ChangePasswordAsync(_currentUserId, request);

                    if (result.IsSuccess)
                    {
                        Logger.LogInformation("用户 {UserName} 密码修改成功，准备自动logout", UserName);

                        // ⭐ Issue #1906修复：调整执行顺序，先完成所有操作，最后关闭对话框

                        // 1. 自动logout（清除Server端和Client端的所有Token）
                        await _authService.LogoutAsync();

                        // 2. 先关闭对话框（避免对话框变空白）
                        SetIsBusy(false);
                        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));

                        // 3. 导航到登录界面
                        EventAggregator.GetEvent<PasswordChangedEvent>().Publish();

                        Logger.LogInformation("用户 {UserName} 已关闭对话框并导航到登录界面", UserName);

                        // 4. 稍微延迟，确保对话框关闭和UI更新完成
                        await Task.Delay(200);

                        // 5. 显示成功消息（此时只有登录界面和MessageBox）
                        await ShowSuccessMessageAsync("密码修改成功！\n\n请使用新密码重新登录。");
                    }
                    else
                    {
                        Logger.LogWarning("用户 {UserName} 密码修改失败: {Message}", UserName, result.Message);
                    }
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

        #endregion
    }
}
