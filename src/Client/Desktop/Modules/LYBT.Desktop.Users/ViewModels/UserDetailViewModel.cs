using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.ViewModels.Components; // Issue #1785: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
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
        // Issue #1785: 使用CommandHandler替代直接Repository访问
        private readonly UserCommandHandler _commandHandler;
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

        /// <summary>
        /// 切换状态命令（Issue #1263: 启用/禁用开关）
        /// </summary>
        public DelegateCommand ToggleStatusCommand { get; }

        public UserDetailViewModel(
            UserCommandHandler commandHandler, // Issue #1785: 注入CommandHandler
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            LYBT.Desktop.Infrastructure.Interfaces.ISessionManager? sessionManager = null,
            LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1785: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            GoBackCommand = new DelegateCommand(ExecuteGoBack);
            EditUserCommand = new DelegateCommand(ExecuteEditUser, CanExecuteEditUser);
            ResetPasswordCommand = new DelegateCommand(ExecuteResetPassword, CanExecuteResetPassword);
            // 异步命令包装（DelegateCommand不直接支持async Task）
            ToggleStatusCommand = new DelegateCommand(async () => await ExecuteToggleStatusAsync(), CanExecuteToggleStatus);
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

                // Issue #1785: 使用CommandHandler查询
                var result = await _commandHandler.GetByIdAsync(UserId);

                if (result.success && result.user != null)
                {
                    User = result.user;
                    PageTitle = $"用户详情 - {User.RealName}";
                    Logger.LogInformation("用户详情加载成功: {UserName}", User.UserName);
                }
                else
                {
                    Logger.LogWarning("未找到用户: UserId={UserId}, ErrorMessage={ErrorMessage}", UserId, result.errorMessage);
                    ErrorMessage = result.errorMessage ?? "未找到该用户信息";
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
                // 刷新命令状态（数据加载完成后，按钮应变为可用）
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 返回用户列表 (Issue #1911修复: Region名称错误)
        /// </summary>
        private void ExecuteGoBack()
        {
            Logger.LogInformation("返回用户列表");
            // 修复：使用正确的Region名称 ContentRegion
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

            NavigateTo("AdminContentRegion", "UserEditView", parameters);
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

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// Issue #1794: 优化方法长度（58→30行）
        /// </summary>
        private async Task ExecuteToggleStatusAsync()
        {
            if (User == null)
            {
                Logger.LogWarning("无法切换状态：用户为空");
                return;
            }

            try
            {
                IsLoading = true;
                var newStatus = CalculateNewStatus();
                var updateDto = CreateUserUpdateDto(newStatus);

                var result = await _commandHandler.UpdateAsync(updateDto);

                if (result.success && result.user != null)
                    HandleToggleSuccess(result.user, newStatus);
                else
                    HandleToggleFailure(result.errorMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换用户状态失败: UserId={UserId}", User?.Id);
                ErrorMessage = $"切换状态失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 计算新状态并记录日志
        /// Issue #1794: 从ExecuteToggleStatus提取，封装状态计算逻辑
        /// </summary>
        private CommonStatus CalculateNewStatus()
        {
            var newStatus = User!.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;

            Logger.LogInformation("开始切换用户状态: UserId={UserId}, 当前状态={CurrentStatus}, 目标状态={NewStatus}",
                User.Id, User.Status, newStatus);

            return newStatus;
        }

        /// <summary>
        /// 创建用户更新DTO
        /// Issue #1794: 从ExecuteToggleStatus提取，封装DTO创建逻辑
        /// </summary>
        private UserInputDto CreateUserUpdateDto(CommonStatus newStatus)
        {
            return new UserInputDto
            {
                Id = User!.Id,
                RealName = User.RealName,
                Role = User.Role,
                Status = newStatus,
                PhoneNumber = User.PhoneNumber,
                Email = User.Email
            };
        }

        /// <summary>
        /// 处理切换成功结果
        /// Issue #1794: 从ExecuteToggleStatus提取，封装成功处理逻辑
        /// </summary>
        private void HandleToggleSuccess(UserDto updatedUser, CommonStatus newStatus)
        {
            User = updatedUser;
            var statusText = newStatus == CommonStatus.Enabled ? "启用" : "禁用";
            StatusMessage = $"用户状态已切换为：{statusText}";
            Logger.LogInformation("用户状态切换成功: UserId={UserId}, 新状态={NewStatus}", User.Id, newStatus);
        }

        /// <summary>
        /// 处理切换失败结果
        /// Issue #1794: 从ExecuteToggleStatus提取，封装失败处理逻辑
        /// </summary>
        private void HandleToggleFailure(string? errorMessage)
        {
            Logger.LogWarning("切换用户状态失败: {ErrorMessage}", errorMessage);
            ErrorMessage = errorMessage ?? "切换状态失败";
        }

        private bool CanExecuteToggleStatus()
        {
            return User != null && !IsLoading;
        }

        /// <summary>
        /// 刷新所有命令的 CanExecute 状态
        /// </summary>
        private void RaiseCanExecuteChanged()
        {
            EditUserCommand.RaiseCanExecuteChanged();
            ResetPasswordCommand.RaiseCanExecuteChanged();
            ToggleStatusCommand.RaiseCanExecuteChanged();
        }
    }
}
