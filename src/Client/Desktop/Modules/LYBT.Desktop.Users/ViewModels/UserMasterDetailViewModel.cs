using System.Collections.ObjectModel;
using System.IO;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户Master-Detail视图模型
    /// OpenSpec: refactor-master-detail-layout
    /// OpenSpec: optimize-entity-data-flow - 列表使用UserListDto
    ///
    /// 合并UserManagementViewModel和UserDetailViewModel功能
    /// </summary>
    public class UserMasterDetailViewModel : MasterDetailViewModelBase<UserListDto, UserDetailModel>
    {
        private readonly UserCommandHandler _commandHandler;
        private readonly IUserRepository _userRepository;
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _prismDialogService;

        #region 筛选属性

        private UserRole? _selectedRoleFilter;
        private CommonStatus? _selectedStatusFilter;
        private bool _showInactiveUsers;

        /// <summary>角色筛选</summary>
        public UserRole? SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            // OpenSpec: fix-infinite-loop - 使用CurrentPage=1避免无限循环（与UserManagementViewModel一致）
            set { if (SetProperty(ref _selectedRoleFilter, value)) CurrentPage = 1; }
        }

        /// <summary>状态筛选</summary>
        public CommonStatus? SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            // OpenSpec: fix-infinite-loop - 使用CurrentPage=1避免无限循环
            set { if (SetProperty(ref _selectedStatusFilter, value)) CurrentPage = 1; }
        }

        /// <summary>显示已禁用用户</summary>
        public bool ShowInactiveUsers
        {
            get => _showInactiveUsers;
            // OpenSpec: fix-infinite-loop - 使用CurrentPage=1避免无限循环
            set { if (SetProperty(ref _showInactiveUsers, value)) CurrentPage = 1; }
        }

        #endregion

        #region 编辑属性

        private string _editUserName = string.Empty;
        private string _editRealName = string.Empty;
        private string _editPinYinCode = string.Empty;
        private string? _editPhoneNumber;
        private string? _editEmail;
        private UserRole _editRole = UserRole.Doctor;
        private CommonStatus _editStatus = CommonStatus.Enabled;

        /// <summary>编辑-用户名</summary>
        public string EditUserName
        {
            get => _editUserName;
            set { if (SetProperty(ref _editUserName, value)) MarkAsModified(); }
        }

        /// <summary>编辑-真实姓名</summary>
        public string EditRealName
        {
            get => _editRealName;
            set
            {
                if (SetProperty(ref _editRealName, value))
                {
                    EditPinYinCode = PinYinHelper.GetPinYinCode(value);
                    MarkAsModified();
                }
            }
        }

        /// <summary>编辑-拼音码（自动生成，可手动修正多音字错误）</summary>
        public string EditPinYinCode
        {
            get => _editPinYinCode;
            set { if (SetProperty(ref _editPinYinCode, value)) MarkAsModified(); }
        }

        /// <summary>编辑-手机号</summary>
        public string? EditPhoneNumber
        {
            get => _editPhoneNumber;
            set { if (SetProperty(ref _editPhoneNumber, value)) MarkAsModified(); }
        }

        /// <summary>编辑-邮箱</summary>
        public string? EditEmail
        {
            get => _editEmail;
            set { if (SetProperty(ref _editEmail, value)) MarkAsModified(); }
        }

        /// <summary>编辑-角色</summary>
        public UserRole EditRole
        {
            get => _editRole;
            set { if (SetProperty(ref _editRole, value)) MarkAsModified(); }
        }

        /// <summary>编辑-状态</summary>
        public CommonStatus EditStatus
        {
            get => _editStatus;
            set { if (SetProperty(ref _editStatus, value)) MarkAsModified(); }
        }

        /// <summary>用户名是否只读（编辑模式下不可修改）</summary>
        public bool IsUserNameReadOnly => CurrentDetail != null && !CurrentDetail.IsNew;

        #endregion

        #region 选项列表

        /// <summary>角色选项</summary>
        public ObservableCollection<UserRole> RoleOptions { get; }

        /// <summary>状态选项</summary>
        public ObservableCollection<CommonStatus> StatusOptions { get; }

        #endregion

        #region 显示属性

        /// <summary>详情标题</summary>
        public string DetailTitle => CurrentDetail == null ? string.Empty :
            CurrentDetail.IsNew ? "新增用户" :
            IsEditMode ? $"编辑用户 - {CurrentDetail.RealName}" :
            $"用户详情 - {CurrentDetail.RealName}";

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        #endregion

        #region 扩展命令

        /// <summary>重置密码命令</summary>
        public DelegateCommand<UserListDto> ResetPasswordCommand { get; private set; } = null!;

        /// <summary>切换用户状态命令</summary>
        public DelegateCommand<UserListDto> ToggleUserStatusCommand { get; private set; } = null!;

        /// <summary>清除筛选命令</summary>
        public DelegateCommand ClearFiltersCommand { get; private set; } = null!;

        /// <summary>导入命令</summary>
        public DelegateCommand ImportCommand { get; private set; } = null!;

        /// <summary>导出命令</summary>
        public DelegateCommand ExportCommand { get; private set; } = null!;

        /// <summary>下载模板命令</summary>
        public DelegateCommand DownloadTemplateCommand { get; private set; } = null!;

        /// <summary>查看审计日志命令</summary>
        public DelegateCommand<UserListDto> ShowAuditLogCommand { get; private set; } = null!;

        /// <summary>恢复命令</summary>
        public DelegateCommand<UserListDto> RestoreCommand { get; private set; } = null!;

        #endregion

        #region 构造函数

        public UserMasterDetailViewModel(
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

            PageTitle = "用户管理";
            PageSize = SystemConstants.DefaultPageSize;

            RoleOptions = new ObservableCollection<UserRole>(Enum.GetValues<UserRole>());
            StatusOptions = new ObservableCollection<CommonStatus>(Enum.GetValues<CommonStatus>());

            InitializeExtendedCommands();
        }

        private void InitializeExtendedCommands()
        {
            ResetPasswordCommand = new DelegateCommand<UserListDto>(async u => await ExecuteResetPasswordAsync(u),
                u => u != null && !IsLoading && u.Status == CommonStatus.Enabled);
            ToggleUserStatusCommand = new DelegateCommand<UserListDto>(async u => await ExecuteToggleUserStatusAsync(u),
                u => u != null && !IsLoading);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters, () => HasActiveFilters);
            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync());
            DownloadTemplateCommand = new DelegateCommand(async () => await ExecuteDownloadTemplateAsync());
            ShowAuditLogCommand = new DelegateCommand<UserListDto>(ExecuteShowAuditLog, u => u != null);
            RestoreCommand = new DelegateCommand<UserListDto>(async u => await RestoreAsync(u),
                u => u != null && !IsLoading && IsAdmin);
        }

        #endregion

        #region 列表数据加载

        protected override async Task<IEnumerable<UserListDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                // OpenSpec: optimize-entity-data-flow - 使用UserListDto轻量DTO
                var result = await _commandHandler.GetPagedListAsync(page, pageSize, searchText);
                if (result.success && result.data != null)
                {
                    TotalCount = result.data.TotalCount;
                    return ApplyFilters(result.data.Items);
                }
                else
                {
                    TotalCount = 0;
                    return new List<UserListDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取用户列表时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, $"获取用户列表 - 模块:{nameof(UserMasterDetailViewModel)}");
                TotalCount = 0;
                return new List<UserListDto>();
            }
        }

        private IEnumerable<UserListDto> ApplyFilters(IEnumerable<UserListDto> items)
        {
            var filteredItems = items.AsEnumerable();
            if (SelectedRoleFilter.HasValue) filteredItems = filteredItems.Where(u => u.Role == SelectedRoleFilter.Value);
            if (SelectedStatusFilter.HasValue) filteredItems = filteredItems.Where(u => u.Status == SelectedStatusFilter.Value);
            if (!ShowInactiveUsers) filteredItems = filteredItems.Where(u => u.Status == CommonStatus.Enabled);
            return filteredItems;
        }

        private bool HasActiveFilters => SelectedRoleFilter.HasValue || SelectedStatusFilter.HasValue || ShowInactiveUsers || !string.IsNullOrEmpty(SearchText);

        private void ExecuteClearFilters()
        {
            SelectedRoleFilter = null;
            SelectedStatusFilter = null;
            ShowInactiveUsers = false;
            SearchText = string.Empty;
        }

        #endregion

        #region Master-Detail抽象方法实现

        protected override async Task<UserDetailModel?> LoadDetailAsync(UserListDto item)
        {
            if (item == null) return null;

            var result = await _commandHandler.GetByIdAsync(item.Id);
            if (!result.success || result.user == null) return null;

            var detail = new UserDetailModel
            {
                Id = result.user.Id,
                UserName = result.user.UserName,
                RealName = result.user.RealName,
                PhoneNumber = result.user.PhoneNumber,
                Email = result.user.Email,
                Role = result.user.Role,
                Status = result.user.Status
            };

            RaisePropertyChanged(nameof(DetailTitle));
            RaisePropertyChanged(nameof(IsUserNameReadOnly));

            return detail;
        }

        protected override async Task<bool> SaveDetailAsync(UserDetailModel detail)
        {
            if (detail == null) return false;

            var dto = new UserInputDto
            {
                Id = detail.Id,
                UserName = EditUserName.Trim(),
                RealName = EditRealName.Trim(),
                PinYinCode = EditPinYinCode?.Trim(),
                PhoneNumber = EditPhoneNumber?.Trim(),
                Email = EditEmail?.Trim(),
                Role = EditRole,
                Status = detail.IsNew ? CommonStatus.Enabled : EditStatus
            };

            var result = detail.IsNew
                ? await _commandHandler.CreateAsync(dto)
                : await _commandHandler.UpdateAsync(dto);

            if (result.success && result.user != null)
            {
                detail.Id = result.user.Id;
                detail.UserName = result.user.UserName;
                detail.RealName = result.user.RealName;
                detail.PinYinCode = result.user.PinYinCode ?? EditPinYinCode ?? string.Empty;
                detail.PhoneNumber = result.user.PhoneNumber;
                detail.Email = result.user.Email;
                detail.Role = result.user.Role;
                detail.Status = result.user.Status;

                RaisePropertyChanged(nameof(DetailTitle));
                return true;
            }

            if (!string.IsNullOrEmpty(result.errorMessage))
            {
                ErrorMessage = result.errorMessage;
            }

            return false;
        }

        protected override async Task<bool> DeleteDetailAsync(UserDetailModel detail)
        {
            if (detail == null || detail.IsNew) return false;

            var result = await _commandHandler.DeleteAsync(detail.Id);
            return result.success;
        }

        protected override UserDetailModel CreateNewDetail()
        {
            var detail = UserDetailModel.CreateNew();
            ClearEditProperties();

            RaisePropertyChanged(nameof(DetailTitle));
            RaisePropertyChanged(nameof(IsUserNameReadOnly));
            return detail;
        }

        protected override UserDetailModel CloneDetail(UserDetailModel detail)
        {
            EditUserName = detail.UserName;
            EditRealName = detail.RealName;
            // 注意：需要在EditRealName之后设置，以覆盖自动生成的值
            EditPinYinCode = detail.PinYinCode;
            EditPhoneNumber = detail.PhoneNumber;
            EditEmail = detail.Email;
            EditRole = detail.Role;
            EditStatus = detail.Status;

            return detail.Clone();
        }

        protected override object? GetDetailId(UserDetailModel detail)
        {
            return detail?.Id;
        }

        #endregion

        #region 删除操作

        protected override async Task OnExecuteDeleteAsync(UserListDto item)
        {
            if (item == null) return;

            try
            {
                var currentUser = SessionManager?.CurrentUser;
                if (currentUser != null && item.Id == currentUser.Id)
                {
                    await ShowWarningMessageAsync("不能删除当前登录用户");
                    return;
                }

                if (!await ShowConfirmationAsync($"确认删除用户 [{item.RealName ?? item.UserName}] 吗？", "删除确认")) return;

                var result = await _commandHandler.DeleteAsync(item.Id);
                if (result.success)
                {
                    await ShowSuccessMessageAsync($"用户 [{item.RealName ?? item.UserName}] 已删除");
                    await RefreshAsync();
                }
                else
                {
                    ErrorMessage = result.errorMessage ?? "删除用户失败";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除用户时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, "删除用户");
            }
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<UserListDto> items)
        {
            if (items == null || items.Count == 0) return;

            var successCount = 0;
            var failureCount = 0;
            var failedItems = new List<string>();
            var currentUser = SessionManager?.CurrentUser;

            foreach (var item in items)
            {
                try
                {
                    if (currentUser != null && item.Id == currentUser.Id)
                    {
                        failureCount++;
                        failedItems.Add($"{item.RealName ?? item.UserName}（不能删除当前登录用户）");
                        continue;
                    }

                    var result = await _commandHandler.DeleteAsync(item.Id);
                    if (result.success) successCount++;
                    else { failureCount++; failedItems.Add($"{item.RealName ?? item.UserName}（{result.errorMessage}）"); }
                }
                catch { failureCount++; failedItems.Add(item.RealName ?? item.UserName); }
            }

            var message = $"批量删除完成！\n成功：{successCount}个\n失败：{failureCount}个";
            if (failureCount > 0 && failedItems.Count > 0)
            {
                message += $"\n\n失败的用户：\n{string.Join("、", failedItems.Take(5))}";
                if (failedItems.Count > 5) message += $"等{failedItems.Count}个";
            }

            if (failureCount > 0) await ShowWarningMessageAsync(message);
            else await ShowSuccessMessageAsync(message);

            if (successCount > 0) await RefreshAsync();
        }

        #endregion

        #region 扩展功能

        private async Task ExecuteResetPasswordAsync(UserListDto user)
        {
            if (user == null) return;
            await ExecuteSafelyAsync(async () =>
            {
                var confirmed = await ShowConfirmationAsync(
                    $"确认重置用户 [{user.RealName ?? user.UserName}] 的密码吗？\n\n密码将被重置为系统配置的默认密码",
                    "重置密码确认");
                if (!confirmed) return;

                var result = await _commandHandler.ResetPasswordAsync(user.Id, null!);
                if (result.success && result.response != null)
                {
                    await ShowSuccessMessageAsync($"用户 [{user.RealName ?? user.UserName}] 的密码已重置\n\n新密码：{result.response.TemporaryPassword}");
                }
                else
                {
                    ErrorMessage = result.errorMessage ?? "重置密码失败";
                }
            }, "重置密码");
        }

        private async Task ExecuteToggleUserStatusAsync(UserListDto user)
        {
            if (user == null) return;
            await ExecuteSafelyAsync(async () =>
            {
                var newStatus = user.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
                var action = newStatus == CommonStatus.Enabled ? "启用" : "禁用";

                // OpenSpec: optimize-entity-data-flow - UserListDto是轻量DTO，需先获取完整数据
                var fullUserResult = await _commandHandler.GetByIdAsync(user.Id);
                if (!fullUserResult.success || fullUserResult.user == null)
                {
                    throw new InvalidOperationException("无法获取用户详细信息");
                }

                var fullUser = fullUserResult.user;
                var updateDto = new UserInputDto
                {
                    Id = fullUser.Id,
                    UserName = fullUser.UserName,
                    RealName = fullUser.RealName,
                    PhoneNumber = fullUser.PhoneNumber,
                    Email = fullUser.Email,
                    Role = fullUser.Role,
                    Status = newStatus
                };

                var result = await _commandHandler.UpdateAsync(updateDto);
                if (result.success)
                {
                    Logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName);
                    await RefreshAsync();
                }
                else
                {
                    throw new InvalidOperationException(result.errorMessage ?? "切换用户状态失败");
                }
            }, user.Status == CommonStatus.Enabled ? "禁用用户" : "启用用户");
        }

        private void ExecuteShowAuditLog(UserListDto? user)
        {
            if (user == null) return;
            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters
            {
                { "EntityType", "user" },
                { "EntityId", user.Id },
                { "EntityDescription", $"用户：{user.RealName ?? user.UserName}" }
            }, _ => { });
        }

        private async Task RestoreAsync(UserListDto user)
        {
            if (user == null) return;
            try
            {
                var confirmed = await ShowConfirmationAsync($"确认恢复用户 [{user.RealName ?? user.UserName}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _userRepository.RestoreAsync(user.Id);
                if (result != null)
                {
                    await ShowSuccessMessageAsync($"用户 '{user.RealName ?? user.UserName}' 已恢复");
                    await RefreshAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("恢复用户失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复用户失败: {UserId}", user.Id);
                await ShowErrorMessageAsync("恢复用户失败");
            }
        }

        #endregion

        #region 导入导出

        private async Task ExecuteImportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _commonDialogService.ShowOpenFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "选择用户导入文件");
                if (string.IsNullOrEmpty(filePath)) return;

                using var fileStream = File.OpenRead(filePath);
                Logger.LogInformation("开始导入用户文件：{FileName}", Path.GetFileName(filePath));

                var users = await Infrastructure.Helpers.ExcelHelper.ParseAsync<UserInputDto>(fileStream, hasHeader: true);
                if (users == null || users.Count == 0)
                {
                    await _commonDialogService.ShowErrorAsync("文件中没有有效的用户数据", "导入用户");
                    return;
                }

                var request = new UserBatchImportInputDto { Users = users, Strategy = DuplicateStrategy.Skip };
                var result = await _userRepository.BatchImportAsync(request);
                if (result == null)
                {
                    await _commonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入用户");
                    return;
                }

                var message = $"导入完成！\n\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n\n成功率：{result.SuccessRate:F1}%";
                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
                    foreach (var failure in result.Failures.Take(3))
                        message += $"\n第{failure.OriginalRowNumber}行 [{failure.UserName}]：{failure.FailureReason}";
                }
                await _commonDialogService.ShowInfoAsync(message, "导入结果");
                if (result.SuccessCount > 0) await RefreshAsync();
            }, "导入用户");
        }

        private async Task ExecuteExportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "导出用户数据",
                    defaultFileName: $"用户数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                Logger.LogInformation("导出用户数据，关键词：{Keyword}", SearchText);
                var allUsers = await _userRepository.SearchAsync(SearchText ?? string.Empty);
                if (allUsers == null || allUsers.Count == 0)
                {
                    await _commonDialogService.ShowErrorAsync("没有可导出的数据", "导出用户");
                    return;
                }

                await Infrastructure.Helpers.ExcelHelper.ExportAsync(allUsers, filePath, "用户数据");
                await _commonDialogService.ShowInfoAsync($"成功导出{allUsers.Count}条用户数据到：\n{filePath}", "导出成功");
            }, "导出用户");
        }

        private async Task ExecuteDownloadTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "保存用户导入模板",
                    defaultFileName: $"用户导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                var sampleData = new List<UserInputDto>
                {
                    new() { UserName = "doctor001", RealName = "张医生", PhoneNumber = "13800138000", Email = "doctor001@example.com", Role = UserRole.Doctor, Status = CommonStatus.Enabled },
                    new() { UserName = "admin001", RealName = "李管理员", PhoneNumber = "13800138001", Email = "admin001@example.com", Role = UserRole.Admin, Status = CommonStatus.Enabled }
                };

                Logger.LogInformation("生成用户导入模板");
                await Infrastructure.Helpers.ExcelHelper.GenerateTemplateAsync(filePath, "用户导入模板", sampleData);
                await _commonDialogService.ShowInfoAsync(
                    $"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入用户」功能导入。\n\n注意：\n1. 用户名必须唯一\n2. 角色可选值：Admin(管理员)、Doctor(医生)、Nurse(护士)\n3. 状态可选值：Enabled(启用)、Disabled(禁用)",
                    "下载成功");
            }, "下载模板");
        }

        #endregion

        #region 辅助方法

        private void ClearEditProperties()
        {
            EditUserName = string.Empty;
            EditRealName = string.Empty;
            EditPinYinCode = string.Empty;
            EditPhoneNumber = null;
            EditEmail = null;
            EditRole = UserRole.Doctor;
            EditStatus = CommonStatus.Enabled;
        }

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();
            ResetPasswordCommand?.RaiseCanExecuteChanged();
            ToggleUserStatusCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            ShowAuditLogCommand?.RaiseCanExecuteChanged();
            RestoreCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
