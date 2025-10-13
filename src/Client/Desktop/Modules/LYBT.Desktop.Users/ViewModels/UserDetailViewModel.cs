using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户详情视图模型 - Issue #1248 完整实现
    /// </summary>
    public class UserDetailViewModel : UnifiedViewModelBase
    {
        private readonly LYBT.Desktop.Users.Interfaces.IUserRepository _userRepository;
        private Guid _userId;
        private UserDto? _user;

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        /// <summary>
        /// 当前用户
        /// </summary>
        public UserDto? User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand GoBackCommand { get; }

        /// <summary>
        /// 编辑用户命令
        /// </summary>
        public DelegateCommand EditUserCommand { get; }

        /// <summary>
        /// 重置密码命令
        /// </summary>
        public DelegateCommand ResetPasswordCommand { get; }

        public UserDetailViewModel(
            LYBT.Desktop.Users.Interfaces.IUserRepository userRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            LYBT.Desktop.Infrastructure.Interfaces.ISessionManager? sessionManager = null,
            LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

            GoBackCommand = new DelegateCommand(ExecuteGoBack);
            EditUserCommand = new DelegateCommand(ExecuteEditUser, CanExecuteEditUser);
            ResetPasswordCommand = new DelegateCommand(ExecuteResetPassword, CanExecuteResetPassword);
        }

        /// <summary>
        /// 处理导航参数（同步）
        /// Issue #1240: 立即设置导航参数
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            if (parameters.ContainsKey("UserId"))
            {
                UserId = parameters.GetValue<Guid>("UserId");
            }
        }

        /// <summary>
        /// 异步初始化数据
        /// Issue #1240: 使用 InitializeAsync 模式
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            if (UserId != Guid.Empty)
            {
                await LoadUserAsync();
            }
        }

        /// <summary>
        /// 加载用户数据
        /// </summary>
        private async Task LoadUserAsync()
        {
            if (UserId == Guid.Empty)
            {
                Logger.LogWarning("LoadUserAsync: UserId 为空");
                return;
            }

            try
            {
                IsLoading = true;

                Logger.LogInformation("开始加载用户详情: UserId={UserId}", UserId);
                User = await _userRepository.GetByIdAsync(UserId);

                if (User != null)
                {
                    PageTitle = $"用户详情 - {User.RealName}";
                    Logger.LogInformation("用户详情加载成功: {UserName}", User.UserName);
                }
                else
                {
                    Logger.LogWarning("未找到用户: UserId={UserId}", UserId);
                    ErrorMessage = "未找到该用户信息";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户详情失败: UserId={UserId}", UserId);
                ErrorMessage = $"加载用户详情失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteGoBack()
        {
            Logger.LogInformation("返回用户列表");
            NavigateBack("ContentRegion");
        }

        private void ExecuteEditUser()
        {
            if (User == null)
            {
                Logger.LogWarning("无法编辑：用户为空");
                return;
            }

            Logger.LogInformation("导航到编辑用户页面: UserId={UserId}", User.Id);

            var parameters = new NavigationParameters
            {
                { "UserId", User.Id }
            };

            NavigateTo("ContentRegion", "UserEditView", parameters);
        }

        private bool CanExecuteEditUser()
        {
            return User != null && !IsLoading;
        }

        private void ExecuteResetPassword()
        {
            if (User == null)
            {
                Logger.LogWarning("无法重置密码：用户为空");
                return;
            }

            Logger.LogInformation("打开重置密码对话框: UserId={UserId}", User.Id);

            // TODO: 使用 Prism IDialogService 打开 ResetPasswordDialog
            // 当前先记录日志
            StatusMessage = "重置密码功能开发中...";
        }

        private bool CanExecuteResetPassword()
        {
            return User != null && !IsLoading;
        }
    }
}
