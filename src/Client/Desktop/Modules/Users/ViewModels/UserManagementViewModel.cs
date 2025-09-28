using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户管理视图模型 - Phase 1架构重构版本
    /// 基于新的ListPageViewModel实现完整的用户管理功能
    /// </summary>
    public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
    {
        #region 依赖服务

        private readonly IUserService _userService;

        #endregion

        #region 筛选属性

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
        public DelegateCommand<UserDto> EditUserCommand { get; private set; }

        /// <summary>
        /// 重置密码命令
        /// </summary>
        public DelegateCommand<UserDto> ResetPasswordCommand { get; private set; }

        /// <summary>
        /// 启用/禁用用户命令
        /// </summary>
        public DelegateCommand<UserDto> ToggleUserStatusCommand { get; private set; }

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand<UserDto> ViewDetailsCommand { get; private set; }

        /// <summary>
        /// 清除筛选命令
        /// </summary>
        public DelegateCommand ClearFiltersCommand { get; private set; }

        #endregion

        #region 构造函数

        public UserManagementViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IUserService userService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化页面属性
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
            EditUserCommand = new DelegateCommand<UserDto>(ExecuteEditUser, CanExecuteEditUser);
            ResetPasswordCommand = new DelegateCommand<UserDto>(async user => await ExecuteResetPasswordAsync(user), CanExecuteResetPassword);
            ToggleUserStatusCommand = new DelegateCommand<UserDto>(async user => await ExecuteToggleUserStatusAsync(user), CanExecuteToggleUserStatus);
            ViewDetailsCommand = new DelegateCommand<UserDto>(ExecuteViewDetails, user => user != null);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters, () => HasActiveFilters);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 获取数据项
        /// </summary>
        protected override async Task<IEnumerable<UserDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogDebug("加载用户数据: 第{Page}页, 每页{PageSize}项, 关键词: {SearchText}", page, pageSize, searchText);

            try
            {
                // 构建查询条件，这里简化处理，实际可能需要更复杂的查询参数传递
                var result = await _userService.GetPagedAsync(page, pageSize, searchText);

                if (result.IsSuccess && result.Data != null)
                {
                    var pagedData = result.Data;

                    // 如果有筛选条件，在客户端进一步过滤（实际项目中应该在服务端处理）
                    var filteredItems = pagedData.Items.AsEnumerable();

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

                    // 设置总数
                    TotalCount = pagedData.TotalCount;
                    return filteredItems;
                }
                else
                {
                    Logger.LogWarning("加载用户数据失败: {ErrorMessage}", result.ErrorMessage);
                    TotalCount = 0;
                    return new List<UserDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户数据时发生异常");
                var context = new ErrorContext { Operation = "加载用户数据", Module = nameof(UserManagementViewModel) };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);

                TotalCount = 0;
                return new List<UserDto>();
            }
        }

        #endregion

        #region 用户操作实现

        /// <summary>
        /// 添加新用户
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            Logger.LogDebug("执行添加新用户");

            // 导航到用户创建页面
            NavigateTo("ContentRegion", "UserCreateView", new Prism.Regions.NavigationParameters
            {
                { "title", "新增用户" }
            });
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("删除用户: {UserId} - {UserName}", user.Id, user.UserName);

            var result = await _userService.DeleteAsync(user.Id);
            if (!result.IsSuccess)
            {
                Logger.LogWarning("删除用户失败: {ErrorMessage}", result.ErrorMessage);
                throw new InvalidOperationException($"删除用户失败: {result.ErrorMessage}");
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
                    var result = await _userService.DeleteAsync(user.Id);
                    if (!result.IsSuccess)
                    {
                        failedUsers.Add($"{user.UserName}: {result.ErrorMessage}");
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
                var errorMessage = $"部分用户删除失败:{string.Join("", failedUsers)}";
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

            NavigateTo("ContentRegion", "UserEditView", new Prism.Regions.NavigationParameters
            {
                { "userId", user.Id },
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

            await ExecuteSafelyAsync(async () =>
            {
                Logger.LogDebug("重置用户密码: {UserId} - {UserName}", user.Id, user.UserName);

                // 这里应该调用密码重置服务，或者打开重置密码对话框
                // 暂时记录日志
                Logger.LogInformation("用户 {UserName} 的密码重置请求已提交", user.UserName);

                // 实际实现可能需要：
                // 1. 打开重置密码对话框
                // 2. 调用密码重置API
                // 3. 发送重置通知

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

                var updateDto = new UserUpdateDto
                {
                    Id = user.Id,
                    Status = newStatus
                };

                var result = await _userService.UpdateAsync(user.Id, updateDto);
                if (result.IsSuccess)
                {
                    Logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName);
                    await LoadPageAsync(); // 刷新数据
                }
                else
                {
                    Logger.LogWarning("{Action}用户失败: {ErrorMessage}", action, result.ErrorMessage);
                    throw new InvalidOperationException($"{action}用户失败: {result.ErrorMessage}");
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

            NavigateTo("ContentRegion", "UserDetailsView", new Prism.Regions.NavigationParameters
            {
                { "userId", user.Id },
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
        /// 是否有活动筛选
        /// </summary>
        private bool HasActiveFilters =>
            SelectedRole.HasValue ||
            SelectedStatus.HasValue ||
            ShowInactiveUsers ||
            !string.IsNullOrEmpty(SearchText);

        #endregion

        #region 命令刷新

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();

            EditUserCommand?.RaiseCanExecuteChanged();
            ResetPasswordCommand?.RaiseCanExecuteChanged();
            ToggleUserStatusCommand?.RaiseCanExecuteChanged();
            ViewDetailsCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
