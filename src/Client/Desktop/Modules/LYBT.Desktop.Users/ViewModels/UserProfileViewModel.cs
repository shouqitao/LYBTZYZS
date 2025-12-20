using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 个人资料编辑视图模型 - Issue #1929 (Sprint 3)
    /// 用户编辑自己的个人资料（Navigation模式）
    /// </summary>
    public class UserProfileViewModel : UnifiedViewModelBase
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
            set
            {
                if (SetProperty(ref _realName, value))
                {
                    UpdateAvatarInitial();
                }
            }
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

        private string _role = string.Empty;
        /// <summary>
        /// 角色（只读）
        /// </summary>
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
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

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand GoBackCommand { get; }

        #endregion

        #region 构造函数

        public UserProfileViewModel(
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

            PageTitle = "个人资料";

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveProfileAsync(), CanSaveProfile)
                .ObservesProperty(() => RealName);

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
                // 获取当前用户 ID
                _currentUserId = _sessionManager?.CurrentUser?.Id ?? Guid.Empty;

                if (_currentUserId == Guid.Empty)
                {
                    Logger.LogError("无法获取当前用户ID");
                    ErrorMessage = "无法获取当前用户信息，请重新登录";
                    return;
                }

                // 加载用户资料
                await LoadUserProfileAsync();

                Logger.LogInformation("UserProfileView 打开，用户ID: {UserId}", _currentUserId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开个人资料页面时发生异常");
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
            Logger.LogInformation("取消编辑个人资料，返回上一页");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 加载用户资料
        /// </summary>
        private async Task LoadUserProfileAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载个人资料...";

                var result = await _commandHandler.GetByIdAsync(_currentUserId);

                if (result.success && result.user != null)
                {
                    UserName = result.user.UserName;
                    RealName = result.user.RealName ?? string.Empty;
                    Email = result.user.Email ?? string.Empty;
                    PhoneNumber = result.user.PhoneNumber ?? string.Empty;
                    Role = result.user.Role.ToString();

                    Logger.LogInformation("用户资料加载成功: {UserName}", UserName);
                }
                else
                {
                    Logger.LogWarning("加载用户资料失败: {ErrorMessage}", result.errorMessage);
                    ErrorMessage = result.errorMessage ?? "加载用户资料失败";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户资料时发生异常");
                ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载", ex);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
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
            return !string.IsNullOrWhiteSpace(RealName) && !IsLoading;
        }

        /// <summary>
        /// 验证表单输入
        /// </summary>
        private bool ValidateForm()
        {
            // 验证真实姓名
            if (string.IsNullOrWhiteSpace(RealName))
            {
                ValidationError = "真实姓名不能为空";
                HasValidationError = true;
                return false;
            }

            // 清除验证错误
            ValidationError = null;
            HasValidationError = false;
            return true;
        }

        /// <summary>
        /// 保存个人资料
        /// </summary>
        private async Task SaveProfileAsync()
        {
            try
            {
                // 验证表单
                if (!ValidateForm())
                {
                    return;
                }

                IsLoading = true;
                StatusMessage = "正在保存个人资料...";

                // 构造更新 DTO
                var updateDto = new UserInputDto
                {
                    Id = _currentUserId,
                    UserName = UserName, // 用户名不可修改，但需要传递
                    RealName = RealName,
                    Email = Email,
                    PhoneNumber = PhoneNumber
                };

                var result = await _commandHandler.UpdateAsync(updateDto);

                if (result.success && result.user != null)
                {
                    await ShowSuccessMessageAsync("个人资料保存成功");

                    // 返回上一页
                    NavigateBack("ContentRegion");

                    Logger.LogInformation("用户 {UserName} 个人资料保存成功", UserName);
                }
                else
                {
                    Logger.LogWarning("保存个人资料失败: {ErrorMessage}", result.errorMessage);
                    ValidationError = result.errorMessage ?? "保存个人资料失败";
                    HasValidationError = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存个人资料时发生异常");
                ValidationError = ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex);
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
