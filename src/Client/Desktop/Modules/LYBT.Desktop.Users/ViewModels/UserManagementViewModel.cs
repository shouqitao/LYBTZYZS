using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Interfaces;
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
    /// 用户管理视图模型 - Phase 1核心功能版本
    /// 基于最新的ListPageViewModel实现完整用户管理功能
    /// </summary>
    public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
    {
        #region 服务依赖

        // Issue #1785: 使用CommandHandler替代直接Repository访问
        private readonly UserCommandHandler _commandHandler;

        #endregion

        #region 筛选条件

        private UserRole? _selectedRole;
        private CommonStatus? _selectedStatus;
        private bool _showInactiveUsers;

        /// <summary>
        /// 选中的角色筛选
        /// </summary>
        public UserRole? SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        /// <summary>
        /// 选中的状态筛选
        /// </summary>
        public CommonStatus? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        /// <summary>
        /// 是否显示已禁用用户
        /// </summary>
        public bool ShowInactiveUsers
        {
            get => _showInactiveUsers;
            set
            {
                if (SetProperty(ref _showInactiveUsers, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        /// <summary>
        /// 角色选项
        /// </summary>
        public IEnumerable<UserRole> RoleOptions { get; }

        /// <summary>
        /// 状态选项
        /// </summary>
        public IEnumerable<CommonStatus> StatusOptions { get; }

        #endregion

        #region 用户特定命令

        /// <summary>
        /// 编辑用户命令
        /// </summary>
        public DelegateCommand<UserDto> EditCommand { get; private set; } = null!;

        /// <summary>
        /// 重置密码命令
        /// </summary>
        public DelegateCommand<UserDto> ResetPasswordCommand { get; private set; } = null!;

        /// <summary>
        /// 启用/禁用用户命令
        /// </summary>
        public DelegateCommand<UserDto> ToggleUserStatusCommand { get; private set; } = null!;

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand<UserDto> ViewDetailsCommand { get; private set; } = null!;

        /// <summary>
        /// 清除筛选命令
        /// </summary>
        public DelegateCommand ClearFiltersCommand { get; private set; } = null!;

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; private set; } = null!;

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        #endregion

        #region 构造函数

        public UserManagementViewModel(
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

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化页面标题
            PageTitle = "用户管理";
            PageSize = 20;

            // 初始化用户特定命令
            InitializeUserCommands();

            Logger.LogDebug("用户管理ViewModel已初始化");
        }

        #endregion

        #region 命令初始化

        private void InitializeUserCommands()
        {
            EditCommand = new DelegateCommand<UserDto>(ExecuteEditUser, CanExecuteEditUser);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, () => CanGoPreviousPage && !IsLoading);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, () => CanGoNextPage && !IsLoading);
            ResetPasswordCommand = new DelegateCommand<UserDto>(async user => await ExecuteResetPasswordAsync(user), CanExecuteResetPassword);
            ToggleUserStatusCommand = new DelegateCommand<UserDto>(async user => await ExecuteToggleUserStatusAsync(user), CanExecuteToggleUserStatus);
            ViewDetailsCommand = new DelegateCommand<UserDto>(ExecuteViewDetails, user => user != null);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters, () => HasActiveFilters);
        }

        #endregion

        #region 暴露基类命令

        /// <summary>
        /// 搜索命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand SearchCommand => base.SearchCommand;

        /// <summary>
        /// 刷新命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand RefreshCommand => base.RefreshCommand;

        /// <summary>
        /// 添加命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand AddCommand => base.AddCommand;

        /// <summary>
        /// 删除命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand<UserDto> DeleteCommand => base.DeleteCommand;

        /// <summary>
        /// 上一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand PreviousPageCommand => base.PreviousPageCommand;

        /// <summary>
        /// 下一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand NextPageCommand => base.NextPageCommand;

        #endregion

        #region 数据加载

        /// <summary>
        /// 获取数据项
        /// </summary>
        protected override async Task<IEnumerable<UserDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogDebug("加载用户列表: 第{Page}页, 每页{PageSize}条, 关键词: {SearchText}", page, pageSize, searchText);

            try
            {
                // Issue #1785: 使用CommandHandler获取分页数据
                var cmdResult = await _commandHandler.GetPagedAsync(page, pageSize, searchText);

                if (cmdResult.success && cmdResult.data != null)
                {
                    var result = cmdResult.data;

                    // 应用筛选条件（在客户端进一步筛选，实际项目应该在服务端处理）
                    var filteredItems = result.Items.AsEnumerable();

                    if (SelectedRole.HasValue)
                    {
                        filteredItems = filteredItems.Where(u => u.Role == SelectedRole.Value);
                    }

                    if (SelectedStatus.HasValue)
                    {
                        filteredItems = filteredItems.Where(u => u.Status == SelectedStatus.Value);
                    }

                    if (!ShowInactiveUsers)
                    {
                        filteredItems = filteredItems.Where(u => u.Status == CommonStatus.Enabled);
                    }

                    // 更新总数
                    TotalCount = result.TotalCount;
                    return filteredItems;
                }
                else
                {
                    Logger.LogWarning("加载用户列表失败: {ErrorMessage}", cmdResult.errorMessage);
                    TotalCount = 0;
                    return new List<UserDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户列表时发生异常");
                var contextMessage = $"加载用户列表 - 模块:{nameof(UserManagementViewModel)}";
                await UserNotificationService!.HandleExceptionAsync(ex, contextMessage);

                TotalCount = 0;
                return new List<UserDto>();
            }
        }

        #endregion

        #region 用户操作实现

        /// <summary>
        /// 添加新用户
        /// </summary>
        protected override Task OnExecuteAddAsync()
        {
            Logger.LogDebug("执行添加新用户");

            // 导航到用户创建页面
            NavigateTo("AdminContentRegion", "UserCreateView", new Prism.Regions.NavigationParameters
            {
                { "title", "创建用户" }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("删除用户: {UserId} - {UserName}", user.Id, user.UserName);

            // Issue #1785: 使用CommandHandler删除
            var result = await _commandHandler.DeleteAsync(user.Id);
            if (!result.success)
            {
                throw new InvalidOperationException(result.errorMessage ?? "删除用户失败");
            }

            Logger.LogInformation("成功删除用户: {UserName}", user.UserName);
        }

        /// <summary>
        /// 批量删除用户
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<UserDto> users)
        {
            Logger.LogDebug("批量删除{Count}个用户", users.Count);

            var failedUsers = new List<string>();

            foreach (var user in users)
            {
                try
                {
                    // Issue #1785: 使用CommandHandler删除
                    var result = await _commandHandler.DeleteAsync(user.Id);
                    if (!result.success)
                    {
                        Logger.LogError("删除用户失败: {UserName}, {ErrorMessage}", user.UserName, result.errorMessage);
                        failedUsers.Add($"{user.UserName}: {result.errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "删除用户失败: {UserName}", user.UserName);
                    failedUsers.Add($"{user.UserName}: {ex.Message}");
                }
            }

            if (failedUsers.Count > 0)
            {
                var errorMessage = $"以下用户删除失败：{string.Join("; ", failedUsers)}";
                Logger.LogWarning("批量删除部分失败: {FailedCount}/{TotalCount}", failedUsers.Count, users.Count);
                throw new InvalidOperationException(errorMessage);
            }

            Logger.LogInformation("成功批量删除{Count}个用户", users.Count);
        }

        #endregion

        #region 用户特定命令实现

        /// <summary>
        /// 编辑用户
        /// </summary>
        private void ExecuteEditUser(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("编辑用户: {UserId} - {UserName}", user.Id, user.UserName);

            NavigateTo("AdminContentRegion", "UserEditView", new Prism.Regions.NavigationParameters
            {
                { "UserId", user.Id },
                { "title", $"编辑用户 - {user.RealName}" }
            });
        }

        /// <summary>
        /// 是否可以编辑用户
        /// </summary>
        private bool CanExecuteEditUser(UserDto user)
        {
            return user != null && !IsLoading;
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        private async Task ExecuteResetPasswordAsync(UserDto user)
        {
            if (user == null) return;

            await ExecuteSafelyAsync(() =>
            {
                Logger.LogDebug("重置用户密码: {UserId} - {UserName}", user.Id, user.UserName);

                // 调用应用的密码重置服务，或者打开密码重置对话框
                // 暂时记录日志
                Logger.LogInformation("用户 {UserName} 的密码重置请求已提交", user.UserName);

                // 实际实现可能需要：
                // 1. 打开密码重置对话框
                // 2. 调用密码重置API
                // 3. 显示成功通知

                return Task.CompletedTask;
            }, "重置密码");
        }

        /// <summary>
        /// 是否可以重置密码
        /// </summary>
        private bool CanExecuteResetPassword(UserDto user)
        {
            return user != null && !IsLoading && user.Status == CommonStatus.Enabled;
        }

        /// <summary>
        /// 切换用户状态
        /// </summary>
        private async Task ExecuteToggleUserStatusAsync(UserDto user)
        {
            if (user == null) return;

            await ExecuteSafelyAsync(async () =>
            {
                var newStatus = user.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
                var action = newStatus == CommonStatus.Enabled ? "启用" : "禁用";

                Logger.LogDebug("{Action}用户: {UserId} - {UserName}", action, user.Id, user.UserName);

                var updateDto = new UserInputDto
                {
                    Id = user.Id,
                    Status = newStatus
                };

                // Issue #1785: 使用CommandHandler更新
                var result = await _commandHandler.UpdateAsync(updateDto);
                if (result.success && result.user != null)
                {
                    Logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName);
                    await LoadPageAsync(); // 刷新列表
                }
                else
                {
                    throw new InvalidOperationException(result.errorMessage ?? "切换用户状态失败");
                }

            }, user.Status == CommonStatus.Enabled ? "禁用用户" : "启用用户");
        }

        /// <summary>
        /// 是否可以切换用户状态
        /// </summary>
        private bool CanExecuteToggleUserStatus(UserDto user)
        {
            return user != null && !IsLoading;
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ExecuteViewDetails(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("查看用户详情: {UserId} - {UserName}", user.Id, user.UserName);

            NavigateTo("AdminContentRegion", "UserDetailView", new Prism.Regions.NavigationParameters
            {
                { "UserId", user.Id },
                { "title", $"用户详情 - {user.RealName}" }
            });
        }

        /// <summary>
        /// 清除筛选
        /// </summary>
        private void ExecuteClearFilters()
        {
            SelectedRole = null;
            SelectedStatus = null;
            ShowInactiveUsers = false;
            SearchText = string.Empty;
        }

        /// <summary>
        /// 是否有筛选
        /// </summary>
        private bool HasActiveFilters =>
            SelectedRole.HasValue ||
            SelectedStatus.HasValue ||
            ShowInactiveUsers ||
            !string.IsNullOrEmpty(SearchText);

        #endregion

        #region 命令刷新

        /// <summary>
        /// 跳转首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 跳转末页
        /// </summary>
        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
        }

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();

            EditCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged();
            ResetPasswordCommand?.RaiseCanExecuteChanged();
            ToggleUserStatusCommand?.RaiseCanExecuteChanged();
            ViewDetailsCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
