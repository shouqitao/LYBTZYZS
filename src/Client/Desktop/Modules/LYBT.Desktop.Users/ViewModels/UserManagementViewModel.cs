using System.IO; // Issue #2003: 文件操作
using System.Windows.Input; // Issue #2003: ICommand
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Interfaces; // Issue #2003: IUserRepository
using LYBT.Desktop.Users.ViewModels.Components; // Issue #1785: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Common; // Issue #1995: PagedResult<T>
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户管理视图模型 - Phase 2统一架构版本
    /// Issue #1995: 继承BaseManagementViewModel泛型基类，享受500ms搜索防抖等统一功能
    /// </summary>
    public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
    {
        #region 服务依赖

        // Issue #1785: 使用CommandHandler替代直接Repository访问
        private readonly UserCommandHandler _commandHandler;

        // Issue #2003: 批量导入功能依赖
        private readonly IUserRepository _userRepository;
        private readonly ICommonDialogService _commonDialogService;

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
                    // Issue #1995: 触发重新加载（基类会自动调用 LoadDataAsync）
                    CurrentPage = 1;
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
                    // Issue #1995: 触发重新加载（基类会自动调用 LoadDataAsync）
                    CurrentPage = 1;
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
                    // Issue #1995: 触发重新加载（基类会自动调用 LoadDataAsync）
                    CurrentPage = 1;
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
        /// <summary>
        /// 新建用户命令
        /// </summary>
        public new DelegateCommand AddCommand { get; private set; } = null!;

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

        
        #endregion

        #region Issue #2003: 批量导入/导出功能

        /// <summary>
        /// 导入用户命令
        /// </summary>
        public ICommand ImportCommand { get; }

        /// <summary>
        /// 导出用户命令
        /// </summary>
        public ICommand ExportCommand { get; }

        /// <summary>
        /// 下载导入模板命令
        /// </summary>
        public ICommand DownloadTemplateCommand { get; }

        #endregion

        #region

        #endregion

        #region 构造函数

        public UserManagementViewModel(
            UserCommandHandler commandHandler, // Issue #1785: 注入CommandHandler
            IUserRepository userRepository, // Issue #2003: 批量导入功能
            ICommonDialogService commonDialogService, // Issue #2003: 批量导入功能
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1785: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // Issue #2003: 批量导入功能
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _commonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));

            // 初始化选项
            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // Issue #1995: 设置分页大小（基类提供）
            PageSize = 20;

            // 初始化用户特定命令
            InitializeUserCommands();

            // Issue #2003: 初始化批量导入/导出命令
            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync());
            DownloadTemplateCommand = new DelegateCommand(async () => await ExecuteDownloadTemplateAsync());

            Logger.LogDebug("用户管理ViewModel已初始化");
        }

        #endregion

        #region 命令初始化

        private void InitializeUserCommands()
        {
            // Issue #1997: 基类不提供AddCommand，需要子类自行实现
            AddCommand = new DelegateCommand(async () => await OnExecuteAddAsync(), () => !IsLoading && !IsBusy)
                .ObservesProperty(() => IsLoading)
                .ObservesProperty(() => IsBusy);

            EditCommand = new DelegateCommand<UserDto>(ExecuteEditUser, CanExecuteEditUser);

            // Issue #2011: 使用 ObservesProperty 防止构造期间无限循环
            
            ResetPasswordCommand = new DelegateCommand<UserDto>(async user => await ExecuteResetPasswordAsync(user), CanExecuteResetPassword);
            ToggleUserStatusCommand = new DelegateCommand<UserDto>(async user => await ExecuteToggleUserStatusAsync(user), CanExecuteToggleUserStatus);
            ViewDetailsCommand = new DelegateCommand<UserDto>(ExecuteViewDetails, user => user != null);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters, () => HasActiveFilters);
        }

        #endregion

        // Issue #1995 注意: UnifiedListViewModelBase 已提供所有必要的命令
        // SearchCommand, RefreshCommand, DeleteCommand, PreviousPageCommand, NextPageCommand 等无需重复定义

        #region 数据加载 - Issue #1995: 实现BaseManagementViewModel抽象方法

        /// <summary>
        /// 加载数据（实现基类抽象方法）
        /// Issue #1995: 从GetItemsAsync重构为LoadDataAsync，返回PagedResult
        /// </summary>
        /// <summary>
        /// 执行新建用户命令
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            // Region Navigation必须在UI线程执行
            Logger.LogInformation("导航到创建用户视图");
            NavigateTo("ContentRegion", "UserDetailView");  // Issue #2168: 统一使用UserDetailView（无参数=Create模式）
            await Task.CompletedTask;
        }

        protected override async Task<IEnumerable<UserDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogDebug("加载用户列表: 第{Page}页, 每页{PageSize}条, 关键词: {SearchText}", page, pageSize, searchText);

            try
            {
                // Issue #1785: 使用CommandHandler获取分页数据
                var cmdResult = await _commandHandler.GetPagedAsync(page, pageSize, searchText);

                if (cmdResult.success && cmdResult.data != null)
                {
                    // 应用客户端筛选（角色、状态）
                    var filteredItems = ApplyFilters(cmdResult.data.Items);
                    return filteredItems;
                }
                else
                {
                    Logger.LogWarning("加载用户列表失败: {ErrorMessage}", cmdResult.errorMessage);
                    return new List<UserDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户列表时发生异常");
                var contextMessage = $"加载用户列表 - 模块:{nameof(UserManagementViewModel)}";
                await UserNotificationService!.HandleExceptionAsync(ex, contextMessage);
                return new List<UserDto>();
            }
        }

        /// <summary>
        /// 删除数据项（实现基类抽象方法）
        /// Issue #1995: 从OnExecuteDeleteAsync重构为DeleteItemAsync
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(UserDto item)
        {
            if (item == null)
            {
                Logger.LogWarning("OnExecuteDeleteAsync: 用户对象为null");
                return;
            }

            Logger.LogDebug("删除用户: {UserId} - {UserName}", item.Id, item.UserName);

            await ExecuteSafelyAsync(async () =>
            {
                // 确认删除
                var confirmed = await ShowConfirmationAsync(
                    $"确认删除用户 [{item.RealName ?? item.UserName}] 吗？",
                    "删除确认");

                if (!confirmed)
                {
                    Logger.LogDebug("用户取消删除, UserId: {UserId}", item.Id);
                    return;
                }

                // Issue #1785: 使用CommandHandler删除
                var result = await _commandHandler.DeleteAsync(item.Id);
                if (result.success)
                {
                    Logger.LogInformation("成功删除用户: {UserName}", item.UserName);
                    await ShowSuccessMessageAsync($"用户 [{item.RealName ?? item.UserName}] 已删除");
                }
                else
                {
                    Logger.LogError("删除用户失败: {UserName}, {ErrorMessage}", item.UserName, result.errorMessage);
                    ErrorMessage = result.errorMessage ?? "删除用户失败";
                }
            }, "删除用户");
        }

        /// <summary>
        /// 批量删除用户（实现基类抽象方法）
        /// Issue #2159: BR-001（权限控制 - 不能删除当前用户）、BR-003（结果反馈）、BR-004（失败不影响其他）
        /// </summary>
        /// <remarks>
        /// 基类ExecuteBatchDeleteAsync已处理确认对话框（BR-002），此方法只负责执行删除逻辑
        /// </remarks>
        protected override async Task OnExecuteBatchDeleteAsync(List<UserDto> items)
        {
            if (items == null || items.Count == 0)
            {
                Logger.LogWarning("OnExecuteBatchDeleteAsync: 用户列表为空");
                return;
            }

            Logger.LogInformation("开始批量删除用户，数量: {Count}", items.Count);

            // BR-003: 统计删除结果
            var successCount = 0;
            var failureCount = 0;
            var failedItems = new List<string>();

            // BR-001: 获取当前登录用户，防止删除自己
            var currentUser = SessionManager?.CurrentUser;
            if (currentUser == null)
            {
                Logger.LogWarning("无法获取当前登录用户信息，跳过批量删除");
                await ShowWarningMessageAsync("无法获取当前登录用户信息，操作已取消");
                return;
            }

            // BR-004: 逐个删除，部分失败不影响其他
            foreach (var item in items)
            {
                try
                {
                    // BR-001: 检查不能删除当前登录用户
                    if (item.Id == currentUser.Id)
                    {
                        failureCount++;
                        failedItems.Add($"{item.RealName ?? item.UserName}（不能删除当前登录用户）");
                        Logger.LogWarning("跳过删除当前登录用户: {UserName}", item.UserName);
                        continue;
                    }

                    // BR-001: 调用CommandHandler.DeleteAsync（包含权限检查）
                    var result = await _commandHandler.DeleteAsync(item.Id);
                    if (result.success)
                    {
                        successCount++;
                        Logger.LogInformation("成功删除用户: {UserName}, 角色: {Role}",
                            item.UserName, item.Role);
                    }
                    else
                    {
                        failureCount++;
                        failedItems.Add($"{item.RealName ?? item.UserName}（{result.errorMessage}）");
                        Logger.LogWarning("删除用户失败: {UserName}, {ErrorMessage}",
                            item.UserName, result.errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    failedItems.Add(item.RealName ?? item.UserName);
                    Logger.LogError(ex, "删除用户时发生异常: {UserName}", item.UserName);
                }
            }

            // BR-003: 生成结果消息
            var message = $"批量删除完成！\n\n" +
                          $"成功：{successCount}个\n" +
                          $"失败：{failureCount}个";

            if (failureCount > 0 && failedItems.Count > 0)
            {
                message += $"\n\n失败的用户：\n{string.Join("、", failedItems.Take(5))}";
                if (failedItems.Count > 5)
                {
                    message += $"等{failedItems.Count}个";
                }
            }

            // BR-003: 显示结果反馈
            if (failureCount > 0)
            {
                await ShowWarningMessageAsync(message);
            }
            else
            {
                await ShowSuccessMessageAsync(message);
            }

            Logger.LogInformation("批量删除完成，成功: {SuccessCount}, 失败: {FailureCount}",
                successCount, failureCount);
        }

        /// <summary>
        /// 应用角色和状态筛选条件
        /// Issue #1794: 从GetItemsAsync提取
        /// </summary>
        private IEnumerable<UserDto> ApplyFilters(IEnumerable<UserDto> items)
        {
            // 应用筛选条件(在客户端进一步筛选,实际项目应该在服务端处理)
            var filteredItems = items.AsEnumerable();

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

            return filteredItems;
        }

        #endregion

        #region 用户操作实现 - Issue #1995: 移除基类已实现的逻辑

        // Issue #1995: OnExecuteDeleteAsync 已由 DeleteItemAsync 替代
        // Issue #1995: OnExecuteBatchDeleteAsync 在 Phase 2 批量操作重构中会有新实现（Task 2.10）

        #endregion

        #region 用户特定命令实现

        /// <summary>
        /// 编辑用户
        /// Issue #1798: 使用Dialog替代Region导航
        /// </summary>
        /// <summary>
        /// 编辑用户 (Issue #1911修复: 异步调用导致UI卡死)
        /// </summary>
        private void ExecuteEditUser(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("编辑用户: {UserId} - {UserName}", user.Id, user.UserName);

            // Issue #1927: 使用Navigation模式代替Dialog
            var parameters = new NavigationParameters
            {
                { "UserId", user.Id }
            };
            NavigateTo("ContentRegion", "UserDetailView", parameters);  // Issue #2168: 统一使用UserDetailView（有UserId=Edit模式）
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
                Logger.LogDebug("准备重置密码, UserId: {UserId}, UserName: {UserName}", 
                    user.Id, user.UserName);

                // Issue #1928 (Sprint 2 优化): 重置密码改为直接重置到默认密码
                // 确认是否重置
                var confirmed = await ShowConfirmationAsync(
                    $"确认重置用户 [{user.RealName ?? user.UserName}] 的密码吗？\n\n密码将被重置为系统配置的默认密码",
                    "重置密码确认");

                if (!confirmed)
                {
                    Logger.LogDebug("用户取消重置密码, UserId: {UserId}", user.Id);
                    return;
                }

                // 调用 CommandHandler 重置密码（传 null 使用服务器配置的默认密码）
                var result = await _commandHandler.ResetPasswordAsync(user.Id, null!);

                if (result.success && result.response != null)
                {
                    Logger.LogInformation("重置密码成功, UserId: {UserId}, UserName: {UserName}",
                        user.Id, user.UserName);

                    // 显示实际重置的密码（从配置文件读取）
                    var resetPassword = result.response.TemporaryPassword;
                    await ShowSuccessMessageAsync($"用户 [{user.RealName ?? user.UserName}] 的密码已重置\n\n新密码：{resetPassword}");
                }
                else
                {
                    Logger.LogWarning("重置密码失败, UserId: {UserId}, ErrorMessage: {ErrorMessage}", 
                        user.Id, result.errorMessage);
                    ErrorMessage = result.errorMessage ?? "重置密码失败";
                }
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

                // 构建完整的UserInputDto，避免AutoMapper覆盖其他字段
                // 注意：UserName 不可更改，但验证器要求必填，所以传递原值
                var updateDto = new UserInputDto
                {
                    Id = user.Id,
                    UserName = user.UserName,  // 保持原值，满足验证器要求
                    RealName = user.RealName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Role = user.Role,
                    Status = newStatus  // 只修改状态
                };

                // Issue #1785: 使用CommandHandler更新
                var result = await _commandHandler.UpdateAsync(updateDto);
                if (result.success && result.user != null)
                {
                    Logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName);
                    // Issue #1995: 使用基类提供的 RefreshAsync
                    await RefreshAsync();
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
        /// <summary>
        /// 查看用户详情 (Issue #1911修复: Region名称错误)
        /// </summary>
        private void ExecuteViewDetails(UserDto user)
        {
            if (user == null) return;

            Logger.LogDebug("查看用户详情: {UserId} - {UserName}", user.Id, user.UserName);

            // Issue #2168: 统一使用UserDetailView（有UserId + ReadOnly=View模式）
            NavigateTo("ContentRegion", "UserDetailView", new Prism.Regions.NavigationParameters
            {
                { "UserId", user.Id },
                { "ReadOnly", true }
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

        #region 命令刷新 - Issue #1995: 使用基类提供的属性

        /// <summary>
        /// 跳转首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            // Issue #1995: 使用基类提供的 PageIndex
            CurrentPage = 1;
        }

        /// <summary>
        /// 跳转末页
        /// </summary>
        private void ExecuteLastPage()
        {
            // Issue #1995: 使用基类提供的 CurrentPage 和 TotalPages
            CurrentPage = TotalPages;
        }

        /// <summary>
        /// 刷新命令状态 - Issue #1995: 重写基类方法
        /// </summary>
        protected override void RefreshCommands()
        {
            base.RefreshCommands(); // 刷新基类命令（RefreshCommand, DeleteCommand, PreviousPageCommand, NextPageCommand）

            // 刷新用户特定命令
            EditCommand?.RaiseCanExecuteChanged();
            ResetPasswordCommand?.RaiseCanExecuteChanged();
            ToggleUserStatusCommand?.RaiseCanExecuteChanged();
            ViewDetailsCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Navigation处理 - Issue #2166

        /// <summary>
        /// 导航到此页面时处理返回参数
        /// Issue #2166: 处理从Create/Edit/ResetPassword返回的刷新请求
        /// </summary>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            var parameters = navigationContext.Parameters;
            if (parameters.GetValue<bool>("RefreshRequired"))
            {
                var operation = parameters.GetValue<string>("Operation");
                var user = parameters.GetValue<UserDto>("User");

                switch (operation)
                {
                    case "UserCreated":
                        Logger.LogInformation("用户创建成功: {UserName}", user?.UserName);
                        _ = RefreshAsync();
                        break;
                    case "UserUpdated":
                        Logger.LogInformation("用户更新成功: {UserName}", user?.UserName);
                        _ = RefreshAsync();
                        break;
                    case "PasswordReset":
                        Logger.LogInformation("密码重置成功: {UserName}", user?.UserName);
                        StatusMessage = $"用户 {user?.RealName} 的密码已重置";
                        break;
                }
            }
        }

        #endregion

        #region Issue #2003: 批量导入/导出功能实现

        /// <summary>
        /// 执行导入用户
        /// Issue #2003 Task 2.10: Desktop主导批量导入模式
        /// </summary>
        private async Task ExecuteImportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // 打开文件选择对话框
                var filePath = await _commonDialogService.ShowOpenFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "选择用户导入文件");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // 读取文件并使用ExcelHelper.ParseAsync解析
                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);

                Logger.LogInformation("开始导入用户文件：{FileName}", fileName);

                // Issue #2003: 使用ExcelHelper.ParseAsync解析Excel为UserInputDto列表
                var users = await Infrastructure.Helpers.ExcelHelper.ParseAsync<UserInputDto>(fileStream, hasHeader: true);

                if (users == null || users.Count == 0)
                {
                    await _commonDialogService.ShowErrorAsync("文件中没有有效的用户数据", "导入用户");
                    return;
                }

                // 组装UserBatchImportRequestDto
                var request = new UserBatchImportRequestDto
                {
                    Users = users,
                    Strategy = LYBT.Shared.Models.Enums.DuplicateStrategy.Skip // 默认策略：跳过重复
                };

                // 调用Server端BatchImportAsync API
                var result = await _userRepository.BatchImportAsync(request);

                if (result == null)
                {
                    await _commonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入用户");
                    return;
                }

                // 显示导入结果
                var message = $"导入完成！\n\n" +
                              $" 成功：{result.SuccessCount}条\n" +
                              $" 失败：{result.FailureCount}条\n" +
                              $"⏭️ 跳过：{result.SkippedCount}条\n\n" +
                              $"成功率：{result.SuccessRate:F1}%";

                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
                    foreach (var failure in result.Failures.Take(3))
                    {
                        message += $"\n第{failure.OriginalRowNumber}行 [{failure.UserName}]：{failure.FailureReason}";
                    }
                }

                await _commonDialogService.ShowInfoAsync(message, "导入结果");

                // 刷新列表
                if (result.SuccessCount > 0)
                {
                    await RefreshAsync();
                }
            }, "导入用户");
        }

        /// <summary>
        /// 执行导出用户
        /// Issue #2003 Task 2.10
        /// </summary>
        private async Task ExecuteExportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // 打开保存文件对话框
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "导出用户数据",
                    defaultFileName: $"用户数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // 获取所有用户数据（使用当前搜索关键词）
                Logger.LogInformation("导出用户数据，关键词：{Keyword}", SearchText);
                var allUsers = await _userRepository.SearchAsync(SearchText ?? string.Empty);

                if (allUsers == null || allUsers.Count == 0)
                {
                    await _commonDialogService.ShowErrorAsync("没有可导出的数据", "导出用户");
                    return;
                }

                // 使用ExcelHelper.ExportAsync导出
                await Infrastructure.Helpers.ExcelHelper.ExportAsync(allUsers, filePath, "用户数据");

                await _commonDialogService.ShowInfoAsync($"成功导出{allUsers.Count}条用户数据到：\n{filePath}", "导出成功");
            }, "导出用户");
        }

        /// <summary>
        /// 执行下载导入模板
        /// Issue #2003 Task 2.10
        /// </summary>
        private async Task ExecuteDownloadTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // 打开保存文件对话框
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "保存用户导入模板",
                    defaultFileName: $"用户导入模板_{DateTime.Now:yyyyMMdd}.xlsx");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // 创建示例数据
                var sampleData = new List<UserInputDto>
                {
                    new UserInputDto
                    {
                        UserName = "doctor001",
                        RealName = "张医生",
                        PhoneNumber = "13800138000",
                        Email = "doctor001@example.com",
                        Role = UserRole.Doctor,
                        Status = CommonStatus.Enabled
                    },
                    new UserInputDto
                    {
                        UserName = "admin001",
                        RealName = "李管理员",
                        PhoneNumber = "13800138001",
                        Email = "admin001@example.com",
                        Role = UserRole.Admin,
                        Status = CommonStatus.Enabled
                    }
                };

                // 使用ExcelHelper.GenerateTemplateAsync生成模板
                Logger.LogInformation("生成用户导入模板");
                await Infrastructure.Helpers.ExcelHelper.GenerateTemplateAsync(filePath, "用户导入模板", sampleData);

                await _commonDialogService.ShowInfoAsync(
                    $"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入用户」功能导入。\n\n注意：\n1. 用户名必须唯一\n2. 角色可选值：Admin(管理员)、Doctor(医生)、Nurse(护士)\n3. 状态可选值：Enabled(启用)、Disabled(禁用)",
                    "下载成功");
            }, "下载模板");
        }

        #endregion
    }
}
