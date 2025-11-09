using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.ViewModels.Components;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 个人资料编辑对话框 ViewModel
    /// Issue #1887-1892 重构：独立的个人信息修改对话框（密码修改已拆分为单独的 ChangePasswordDialog）
    /// </summary>
    [Obsolete("此Dialog已废弃，请使用 UserProfileView 替代。Epic #1926 Sprint 4。", true)]
    public class UserProfileDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        private readonly UserCommandHandler _commandHandler;
        private readonly ISessionManager _sessionManager;
        private Guid _currentUserId;

        #region 属性

        private string _avatarInitial = string.Empty;
        /// <summary>
        /// 头像首字母（无头像时显示）
        /// </summary>
        public string AvatarInitial
        {
            get => _avatarInitial;
            set => SetProperty(ref _avatarInitial, value);
        }

        private string _username = string.Empty;
        /// <summary>
        /// 用户名（只读）
        /// </summary>
        public string UserName
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    UpdateAvatarInitial();
                }
            }
        }

        private string _realName = string.Empty;
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName
        {
            get => _realName;
            set => SetProperty(ref _realName, value);
        }

        private string _email = string.Empty;
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phoneNumber = string.Empty;
        /// <summary>
        /// 电话号码
        /// </summary>
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        #endregion

        #region IDialogAware

        public string Title => "个人资料";

        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public UserProfileDialogViewModel(
            UserCommandHandler commandHandler,
            ISessionManager sessionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveProfileAsync(), CanSaveProfile)
                .ObservesProperty(() => RealName);

            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel)));
        }

        #endregion

        #region IDialogAware 实现

        public bool CanCloseDialog() => !IsBusy;

        public void OnDialogClosed()
        {
            // 清理资源
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                // 获取当前用户 ID
                _currentUserId = _sessionManager?.CurrentUser?.Id ?? Guid.Empty;

                if (_currentUserId == Guid.Empty)
                {
                    Logger.LogError("无法获取当前用户ID，关闭对话框");
                    _ = ShowErrorMessageAsync("无法获取当前用户信息，请重新登录");
                    // 延迟关闭对话框，让用户看到错误消息
                    Task.Delay(1500).ContinueWith(_ =>
                    {
                        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
                    });
                    return;
                }

                // 加载用户资料
                _ = LoadUserProfileAsync();

                Logger.LogInformation("UserProfileDialog 打开，用户ID: {UserId}", _currentUserId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开个人资料对话框时发生异常");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载用户资料
        /// </summary>
        private async Task LoadUserProfileAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载个人资料...");

                var result = await _commandHandler.GetByIdAsync(_currentUserId);

                if (result.success && result.user != null)
                {
                    UserName = result.user.UserName;
                    RealName = result.user.RealName ?? string.Empty;
                    Email = result.user.Email ?? string.Empty;
                    PhoneNumber = result.user.PhoneNumber ?? string.Empty;

                    Logger.LogInformation("用户资料加载成功: {UserName}", UserName);
                }
                else
                {
                    Logger.LogWarning("加载用户资料失败: {ErrorMessage}", result.errorMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户资料时发生异常");
                await ShowErrorMessageAsync($"加载失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 更新头像首字母
        /// </summary>
        private void UpdateAvatarInitial()
        {
            if (!string.IsNullOrWhiteSpace(RealName))
            {
                AvatarInitial = RealName.Substring(0, 1).ToUpper();
            }
            else if (!string.IsNullOrWhiteSpace(UserName))
            {
                AvatarInitial = UserName.Substring(0, 1).ToUpper();
            }
            else
            {
                AvatarInitial = "?";
            }
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        private bool CanSaveProfile()
        {
            return !string.IsNullOrWhiteSpace(RealName);
        }

        /// <summary>
        /// 保存个人资料
        /// </summary>
        private async Task SaveProfileAsync()
        {
            try
            {

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(RealName))
                {
                    return;
                }

                SetIsBusy(true, "正在保存个人资料...");

                // 构造更新 DTO
                var updateDto = new LYBT.Shared.Models.Contracts.Users.UserInputDto
                {
                    Id = _currentUserId,
                    UserName = UserName, // 用户名不可修改，但需要传递
                    RealName = RealName,
                    Email = Email,
                    PhoneNumber = PhoneNumber
                    // Department 和 Position 当前接口可能不支持
                };

                var result = await _commandHandler.UpdateAsync(updateDto);

                if (result.success)
                {
                    await ShowSuccessMessageAsync("个人资料保存成功");
                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
                    Logger.LogInformation("用户 {UserName} 个人资料保存成功", UserName);
                }
                else
                {
                    Logger.LogWarning("保存个人资料失败: {ErrorMessage}", result.errorMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存个人资料时发生异常");
                await ShowErrorMessageAsync($"保存失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion
    }
}
