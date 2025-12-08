using System.IO;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>用户管理视图模型</summary>
    public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
    {
        private readonly UserCommandHandler _commandHandler;
        private readonly IUserRepository _userRepository;
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _prismDialogService;

        private UserRole? _selectedRole;
        private CommonStatus? _selectedStatus;
        private bool _showInactiveUsers;

        public UserRole? SelectedRole
        {
            get => _selectedRole;
            set { if (SetProperty(ref _selectedRole, value)) CurrentPage = 1; }
        }

        public CommonStatus? SelectedStatus
        {
            get => _selectedStatus;
            set { if (SetProperty(ref _selectedStatus, value)) CurrentPage = 1; }
        }

        public bool ShowInactiveUsers
        {
            get => _showInactiveUsers;
            set { if (SetProperty(ref _showInactiveUsers, value)) CurrentPage = 1; }
        }

        public IEnumerable<UserRole> RoleOptions { get; }
        public IEnumerable<CommonStatus> StatusOptions { get; }

        /// <summary>是否为管理员（Admin或SuperAdmin角色）- OpenSpec: optimize-module-list-ui UI-022</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        public new DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand<UserDto> EditCommand { get; private set; } = null!;
        public DelegateCommand<UserDto> ResetPasswordCommand { get; private set; } = null!;
        public DelegateCommand<UserDto> ToggleUserStatusCommand { get; private set; } = null!;
        public DelegateCommand<UserDto> ViewDetailsCommand { get; private set; } = null!;
        public DelegateCommand ClearFiltersCommand { get; private set; } = null!;
        public DelegateCommand<UserDto> ShowAuditLogCommand { get; private set; } = null!;
        /// <summary>恢复软删除数据命令 - OpenSpec: optimize-module-list-ui UI-022</summary>
        public DelegateCommand<UserDto> RestoreCommand { get; private set; } = null!;
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand DownloadTemplateCommand { get; }

        public UserManagementViewModel(
            UserCommandHandler commandHandler,
            IUserRepository userRepository,
            ICommonDialogService commonDialogService,
            IDialogService prismDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _commonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));

            RoleOptions = Enum.GetValues<UserRole>();
            StatusOptions = Enum.GetValues<CommonStatus>();
            PageSize = 20;

            InitializeUserCommands();
            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync());
            DownloadTemplateCommand = new DelegateCommand(async () => await ExecuteDownloadTemplateAsync());

            Logger.LogDebug("用户管理ViewModel已初始化");
        }

        private void InitializeUserCommands()
        {
            AddCommand = new DelegateCommand(async () => await OnExecuteAddAsync(), () => !IsLoading && !IsBusy)
                .ObservesProperty(() => IsLoading).ObservesProperty(() => IsBusy);
            EditCommand = new DelegateCommand<UserDto>(ExecuteEditUser, u => u != null && !IsLoading);
            ResetPasswordCommand = new DelegateCommand<UserDto>(async u => await ExecuteResetPasswordAsync(u), u => u != null && !IsLoading && u.Status == CommonStatus.Enabled);
            ToggleUserStatusCommand = new DelegateCommand<UserDto>(async u => await ExecuteToggleUserStatusAsync(u), u => u != null && !IsLoading);
            ViewDetailsCommand = new DelegateCommand<UserDto>(ExecuteViewDetails, u => u != null);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters, () => HasActiveFilters);
            ShowAuditLogCommand = new DelegateCommand<UserDto>(ExecuteShowAuditLog, u => u != null);
            // OpenSpec: optimize-module-list-ui UI-022 - 初始化恢复命令
            RestoreCommand = new DelegateCommand<UserDto>(async u => await RestoreAsync(u), u => u != null && !IsLoading && IsAdmin);
        }

        protected override async Task OnExecuteAddAsync()
        {
            Logger.LogInformation("导航到创建用户视图");
            NavigateTo("ContentRegion", "UserDetailView");
            await Task.CompletedTask;
        }

        protected override async Task<IEnumerable<UserDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogDebug("加载用户列表: 第{Page}页, 每页{PageSize}条, 关键词: {SearchText}", page, pageSize, searchText);
            try
            {
                var cmdResult = await _commandHandler.GetPagedAsync(page, pageSize, searchText);
                if (cmdResult.success && cmdResult.data != null) return ApplyFilters(cmdResult.data.Items);
                Logger.LogWarning("加载用户列表失败: {ErrorMessage}", cmdResult.errorMessage);
                return new List<UserDto>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载用户列表时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, $"加载用户列表 - 模块:{nameof(UserManagementViewModel)}");
                return new List<UserDto>();
            }
        }

        protected override async Task OnExecuteDeleteAsync(UserDto item)
        {
            if (item == null) { Logger.LogWarning("OnExecuteDeleteAsync: 用户对象为null"); return; }
            Logger.LogDebug("删除用户: {UserId} - {UserName}", item.Id, item.UserName);

            await ExecuteSafelyAsync(async () =>
            {
                var confirmed = await ShowConfirmationAsync($"确认删除用户 [{item.RealName ?? item.UserName}] 吗？", "删除确认");
                if (!confirmed) { Logger.LogDebug("用户取消删除, UserId: {UserId}", item.Id); return; }

                var result = await _commandHandler.DeleteAsync(item.Id);
                if (result.success)
                {
                    Logger.LogInformation("成功删除用户: {UserName}", item.UserName);
                    await ShowSuccessMessageAsync($"用户 [{item.RealName ?? item.UserName}] 已删除");
                    await RefreshAsync();
                }
                else
                {
                    Logger.LogError("删除用户失败: {UserName}, {ErrorMessage}", item.UserName, result.errorMessage);
                    ErrorMessage = result.errorMessage ?? "删除用户失败";
                }
            }, "删除用户");
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<UserDto> items)
        {
            if (items == null || items.Count == 0) { Logger.LogWarning("OnExecuteBatchDeleteAsync: 用户列表为空"); return; }
            Logger.LogInformation("开始批量删除用户，数量: {Count}", items.Count);

            var successCount = 0;
            var failureCount = 0;
            var failedItems = new List<string>();
            var currentUser = SessionManager?.CurrentUser;

            if (currentUser == null) { await ShowWarningMessageAsync("无法获取当前登录用户信息，操作已取消"); return; }

            foreach (var item in items)
            {
                try
                {
                    if (item.Id == currentUser.Id)
                    {
                        failureCount++; failedItems.Add($"{item.RealName ?? item.UserName}（不能删除当前登录用户）");
                        Logger.LogWarning("跳过删除当前登录用户: {UserName}", item.UserName);
                        continue;
                    }

                    var result = await _commandHandler.DeleteAsync(item.Id);
                    if (result.success) { successCount++; Logger.LogInformation("成功删除用户: {UserName}", item.UserName); }
                    else { failureCount++; failedItems.Add($"{item.RealName ?? item.UserName}（{result.errorMessage}）"); }
                }
                catch (Exception ex)
                {
                    failureCount++; failedItems.Add(item.RealName ?? item.UserName);
                    Logger.LogError(ex, "删除用户时发生异常: {UserName}", item.UserName);
                }
            }

            var message = $"批量删除完成！\n\n成功：{successCount}个\n失败：{failureCount}个";
            if (failureCount > 0 && failedItems.Count > 0)
            {
                message += $"\n\n失败的用户：\n{string.Join("、", failedItems.Take(5))}";
                if (failedItems.Count > 5) message += $"等{failedItems.Count}个";
            }

            if (failureCount > 0) await ShowWarningMessageAsync(message);
            else await ShowSuccessMessageAsync(message);
            Logger.LogInformation("批量删除完成，成功: {SuccessCount}, 失败: {FailureCount}", successCount, failureCount);
            // 刷新列表显示最新数据
            if (successCount > 0) await RefreshAsync();
        }

        private IEnumerable<UserDto> ApplyFilters(IEnumerable<UserDto> items)
        {
            var filteredItems = items.AsEnumerable();
            if (SelectedRole.HasValue) filteredItems = filteredItems.Where(u => u.Role == SelectedRole.Value);
            if (SelectedStatus.HasValue) filteredItems = filteredItems.Where(u => u.Status == SelectedStatus.Value);
            if (!ShowInactiveUsers) filteredItems = filteredItems.Where(u => u.Status == CommonStatus.Enabled);
            return filteredItems;
        }

        private void ExecuteEditUser(UserDto user)
        {
            if (user == null) return;
            Logger.LogDebug("编辑用户: {UserId} - {UserName}", user.Id, user.UserName);
            NavigateTo("ContentRegion", "UserDetailView", new NavigationParameters { { "UserId", user.Id } });
        }

        private async Task ExecuteResetPasswordAsync(UserDto user)
        {
            if (user == null) return;
            await ExecuteSafelyAsync(async () =>
            {
                Logger.LogDebug("准备重置密码, UserId: {UserId}", user.Id);
                var confirmed = await ShowConfirmationAsync($"确认重置用户 [{user.RealName ?? user.UserName}] 的密码吗？\n\n密码将被重置为系统配置的默认密码", "重置密码确认");
                if (!confirmed) { Logger.LogDebug("用户取消重置密码"); return; }

                var result = await _commandHandler.ResetPasswordAsync(user.Id, null!);
                if (result.success && result.response != null)
                {
                    Logger.LogInformation("重置密码成功, UserId: {UserId}", user.Id);
                    await ShowSuccessMessageAsync($"用户 [{user.RealName ?? user.UserName}] 的密码已重置\n\n新密码：{result.response.TemporaryPassword}");
                }
                else { ErrorMessage = result.errorMessage ?? "重置密码失败"; }
            }, "重置密码");
        }

        private async Task ExecuteToggleUserStatusAsync(UserDto user)
        {
            if (user == null) return;
            await ExecuteSafelyAsync(async () =>
            {
                var newStatus = user.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
                var action = newStatus == CommonStatus.Enabled ? "启用" : "禁用";
                Logger.LogDebug("{Action}用户: {UserId}", action, user.Id);

                var updateDto = new UserInputDto
                {
                    Id = user.Id, UserName = user.UserName, RealName = user.RealName,
                    PhoneNumber = user.PhoneNumber, Email = user.Email, Role = user.Role, Status = newStatus
                };

                var result = await _commandHandler.UpdateAsync(updateDto);
                if (result.success) { Logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName); await RefreshAsync(); }
                else throw new InvalidOperationException(result.errorMessage ?? "切换用户状态失败");
            }, user.Status == CommonStatus.Enabled ? "禁用用户" : "启用用户");
        }

        private void ExecuteViewDetails(UserDto user)
        {
            if (user == null) return;
            Logger.LogDebug("查看用户详情: {UserId}", user.Id);
            NavigateTo("ContentRegion", "UserDetailView", new NavigationParameters { { "UserId", user.Id }, { "ReadOnly", true } });
        }

        private void ExecuteClearFilters() { SelectedRole = null; SelectedStatus = null; ShowInactiveUsers = false; SearchText = string.Empty; }
        private bool HasActiveFilters => SelectedRole.HasValue || SelectedStatus.HasValue || ShowInactiveUsers || !string.IsNullOrEmpty(SearchText);

        protected override void RefreshCommands()
        {
            base.RefreshCommands();
            EditCommand?.RaiseCanExecuteChanged();
            ResetPasswordCommand?.RaiseCanExecuteChanged();
            ToggleUserStatusCommand?.RaiseCanExecuteChanged();
            ViewDetailsCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }

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
                    case "UserCreated": Logger.LogInformation("用户创建成功: {UserName}", user?.UserName); _ = RefreshAsync(); break;
                    case "UserUpdated": Logger.LogInformation("用户更新成功: {UserName}", user?.UserName); _ = RefreshAsync(); break;
                    case "PasswordReset": Logger.LogInformation("密码重置成功: {UserName}", user?.UserName); StatusMessage = $"用户 {user?.RealName} 的密码已重置"; break;
                }
            }
        }

        private async Task ExecuteImportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _commonDialogService.ShowOpenFileDialogAsync(filter: "Excel文件|*.xlsx", title: "选择用户导入文件");
                if (string.IsNullOrEmpty(filePath)) return;

                using var fileStream = File.OpenRead(filePath);
                Logger.LogInformation("开始导入用户文件：{FileName}", Path.GetFileName(filePath));

                var users = await Infrastructure.Helpers.ExcelHelper.ParseAsync<UserInputDto>(fileStream, hasHeader: true);
                if (users == null || users.Count == 0) { await _commonDialogService.ShowErrorAsync("文件中没有有效的用户数据", "导入用户"); return; }

                var request = new UserBatchImportRequestDto { Users = users, Strategy = DuplicateStrategy.Skip };
                var result = await _userRepository.BatchImportAsync(request);
                if (result == null) { await _commonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入用户"); return; }

                var message = $"导入完成！\n\n 成功：{result.SuccessCount}条\n 失败：{result.FailureCount}条\n⏭️ 跳过：{result.SkippedCount}条\n\n成功率：{result.SuccessRate:F1}%";
                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
                    foreach (var failure in result.Failures.Take(3)) message += $"\n第{failure.OriginalRowNumber}行 [{failure.UserName}]：{failure.FailureReason}";
                }
                await _commonDialogService.ShowInfoAsync(message, "导入结果");
                if (result.SuccessCount > 0) await RefreshAsync();
            }, "导入用户");
        }

        private async Task ExecuteExportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(filter: "Excel文件|*.xlsx", title: "导出用户数据", defaultFileName: $"用户数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                Logger.LogInformation("导出用户数据，关键词：{Keyword}", SearchText);
                var allUsers = await _userRepository.SearchAsync(SearchText ?? string.Empty);
                if (allUsers == null || allUsers.Count == 0) { await _commonDialogService.ShowErrorAsync("没有可导出的数据", "导出用户"); return; }

                await Infrastructure.Helpers.ExcelHelper.ExportAsync(allUsers, filePath, "用户数据");
                await _commonDialogService.ShowInfoAsync($"成功导出{allUsers.Count}条用户数据到：\n{filePath}", "导出成功");
            }, "导出用户");
        }

        private async Task ExecuteDownloadTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(filter: "Excel文件|*.xlsx", title: "保存用户导入模板", defaultFileName: $"用户导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                var sampleData = new List<UserInputDto>
                {
                    new() { UserName = "doctor001", RealName = "张医生", PhoneNumber = "13800138000", Email = "doctor001@example.com", Role = UserRole.Doctor, Status = CommonStatus.Enabled },
                    new() { UserName = "admin001", RealName = "李管理员", PhoneNumber = "13800138001", Email = "admin001@example.com", Role = UserRole.Admin, Status = CommonStatus.Enabled }
                };

                Logger.LogInformation("生成用户导入模板");
                await Infrastructure.Helpers.ExcelHelper.GenerateTemplateAsync(filePath, "用户导入模板", sampleData);
                await _commonDialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入用户」功能导入。\n\n注意：\n1. 用户名必须唯一\n2. 角色可选值：Admin(管理员)、Doctor(医生)、Nurse(护士)\n3. 状态可选值：Enabled(启用)、Disabled(禁用)", "下载成功");
            }, "下载模板");
        }

        private void ExecuteShowAuditLog(UserDto? user)
        {
            if (user == null) return;
            Logger.LogInformation("查看用户审计日志：{UserId}", user.Id);
            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters { { "EntityType", "user" }, { "EntityId", user.Id }, { "EntityDescription", $"用户：{user.RealName ?? user.UserName}" } }, _ => { });
        }

        /// <summary>恢复软删除的用户 - OpenSpec: optimize-module-list-ui UI-022</summary>
        private async Task RestoreAsync(UserDto user)
        {
            if (user == null) return;
            try
            {
                Logger.LogInformation("恢复软删除用户: {UserId} - {UserName}", user.Id, user.UserName);
                var confirmed = await ShowConfirmationAsync($"确认恢复用户 [{user.RealName ?? user.UserName}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _userRepository.RestoreAsync(user.Id);
                if (result != null)
                {
                    Logger.LogInformation("用户已恢复: {UserName}", user.UserName);
                    await ShowSuccessMessageAsync($"用户 '{user.RealName ?? user.UserName}' 已恢复");
                    await RefreshAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("恢复用户失败");
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "恢复用户失败: {UserId}", user.Id); await ShowErrorMessageAsync("恢复用户失败"); }
        }
    }
}
