using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Desktop.Utilities.Excel;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels;

/// <summary>
/// 用户Master-Detail视图模型（组合模式）
/// OpenSpec: refactor-viewmodel-composition
///
/// 使用IMasterDetailServices实现组合模式
/// </summary>
public partial class UserMasterDetailViewModel : MasterDetailViewModelBase<UserListDto, UserDetailModel>
{
    private readonly UserService _commandHandler;
    private readonly IUserRepository _userRepository;
    private readonly IDialogService _prismDialogService;
    private readonly ICommonDialogService? _commonDialogService;
    private readonly ISessionManager? _sessionManager;

    #region 筛选属性

    private UserRole? _selectedRoleFilter;
    private CommonStatus? _selectedStatusFilter;
    private bool _showInactiveUsers;

    /// <summary>角色筛选</summary>
    public UserRole? SelectedRoleFilter
    {
        get => _selectedRoleFilter;
        set
        {
            if (SetProperty(ref _selectedRoleFilter, value))
            {
                Services.Pagination.GoToFirstPage();
            }
        }
    }

    /// <summary>状态筛选</summary>
    public CommonStatus? SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                Services.Pagination.GoToFirstPage();
            }
        }
    }

    /// <summary>显示已禁用用户</summary>
    public bool ShowInactiveUsers
    {
        get => _showInactiveUsers;
        set
        {
            if (SetProperty(ref _showInactiveUsers, value))
            {
                Services.Pagination.GoToFirstPage();
            }
        }
    }

    /// <summary>是否有活动筛选</summary>
    private bool HasActiveFilters =>
        SelectedRoleFilter.HasValue || SelectedStatusFilter.HasValue ||
        ShowInactiveUsers || !string.IsNullOrEmpty(SearchText);

    #endregion

    #region 扩展属性

    /// <summary>是否为管理员</summary>
    public bool IsAdmin => _sessionManager?.HasPermission(UserRole.Admin) == true;

    /// <summary>用户名是否只读（编辑模式下不可修改）</summary>
    public bool IsUserNameReadOnly => CurrentDetail != null && !IsNew;

    /// <summary>角色选项</summary>
    public ObservableCollection<UserRole> RoleOptions { get; } = new(Enum.GetValues<UserRole>());

    /// <summary>状态选项</summary>
    public ObservableCollection<CommonStatus> StatusOptions { get; } = new(Enum.GetValues<CommonStatus>());

    /// <summary>详情标题</summary>
    public string DetailTitle
    {
        get
        {
            if (CurrentDetail == null) return "用户详情";
            if (IsNew) return "新增用户";
            return IsEditMode ? $"编辑用户 - {CurrentDetail.RealName}" : $"用户详情 - {CurrentDetail.RealName}";
        }
    }

    #endregion

    public UserMasterDetailViewModel(
        IMasterDetailServices<UserListDto, UserDetailModel> services,
        UserService commandHandler,
        IUserRepository userRepository,
        IDialogService prismDialogService,
        ILoggerFactory loggerFactory,
        ISessionManager? sessionManager = null,
        ICommonDialogService? commonDialogService = null)
        : base(services, loggerFactory)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
        _sessionManager = sessionManager;
        _commonDialogService = commonDialogService;

        PageTitle = "用户管理";

        // 监听属性变化
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(CurrentDetail) or nameof(IsEditMode))
            {
                OnPropertyChanged(nameof(DetailTitle));
                OnPropertyChanged(nameof(IsUserNameReadOnly));
            }
        };
    }

    #region 基类抽象方法实现

    /// <summary>加载列表数据</summary>
    protected override async Task LoadListAsync()
    {
        Logger.LogInformation("用户搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'",
            CurrentPage, PageSize, SearchText);

        try
        {
            await Services.Loading.ExecuteWithLoadingAsync(async () =>
            {
                var result = await _commandHandler.GetPagedAsync(CurrentPage, PageSize, SearchText);
                if (result.success && result.data != null)
                {
                    Services.Pagination.TotalCount = result.data.TotalCount;

                    Items.Clear();
                    var filteredItems = ApplyFilters(result.data.Items);
                    foreach (var item in filteredItems)
                    {
                        Items.Add(item);
                    }
                }
                else
                {
                    Services.Pagination.TotalCount = 0;
                    Items.Clear();
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取用户列表时发生异常");
            Services.ErrorHandler.HandleException(ex, "获取用户列表");
        }
    }

    private IEnumerable<UserListDto> ApplyFilters(IEnumerable<UserListDto> items)
    {
        var filteredItems = items.AsEnumerable();
        if (SelectedRoleFilter.HasValue)
            filteredItems = filteredItems.Where(u => u.Role == SelectedRoleFilter.Value);
        if (SelectedStatusFilter.HasValue)
            filteredItems = filteredItems.Where(u => u.Status == SelectedStatusFilter.Value);
        if (!ShowInactiveUsers)
            filteredItems = filteredItems.Where(u => u.Status == CommonStatus.Enabled);
        return filteredItems;
    }

    /// <summary>加载详情数据</summary>
    protected override async Task LoadDetailAsync(UserListDto item)
    {
        try
        {
            var result = await _commandHandler.GetByIdAsync(item.Id);
            if (!result.success || result.user == null)
            {
                await Services.Dialog.ShowErrorAsync($"用户 '{item.UserName}' 不存在或已被删除", "加载失败");
                return;
            }

            var detail = new UserDetailModel
            {
                Id = result.user.Id,
                UserName = result.user.UserName,
                RealName = result.user.RealName,
                PinYinCode = result.user.PinYinCode ?? string.Empty,
                PhoneNumber = result.user.PhoneNumber,
                Email = result.user.Email,
                Role = result.user.Role,
                Status = result.user.Status,
                LastLoginTime = result.user.LastLoginTime,
                CreatedAt = result.user.CreatedAt,
                UpdatedAt = result.user.UpdatedAt,
                Remark = result.user.Remark
            };

            Services.DetailEditor.LoadDetail(detail);
            OnPropertyChanged(nameof(DetailTitle));
            OnPropertyChanged(nameof(IsUserNameReadOnly));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载用户详情失败: {UserId}", item.Id);
            Services.ErrorHandler.HandleException(ex, "加载用户详情");
        }
    }

    /// <summary>创建新详情实例</summary>
    protected override UserDetailModel CreateNewDetail()
    {
        var detail = UserDetailModel.CreateNew();
        OnPropertyChanged(nameof(DetailTitle));
        OnPropertyChanged(nameof(IsUserNameReadOnly));
        return detail;
    }

    /// <summary>保存详情</summary>
    protected override async Task<bool> SaveDetailAsync(UserDetailModel detail)
    {
        if (string.IsNullOrWhiteSpace(detail.UserName))
        {
            await Services.Dialog.ShowErrorAsync("用户名不能为空", "验证失败");
            return false;
        }

        try
        {
            var dto = new UserInputDto
            {
                Id = detail.Id,
                UserName = detail.UserName.Trim(),
                RealName = detail.RealName?.Trim() ?? string.Empty,
                PinYinCode = detail.PinYinCode?.Trim(),
                PhoneNumber = detail.PhoneNumber?.Trim(),
                Email = detail.Email?.Trim(),
                Role = detail.Role
            };

            var result = IsNew
                ? await _commandHandler.CreateAsync(dto)
                : await _commandHandler.UpdateAsync(dto);

            if (result.success && result.user != null)
            {
                // 回填服务器返回数据
                detail.Id = result.user.Id;
                detail.UserName = result.user.UserName;
                detail.RealName = result.user.RealName;
                detail.PinYinCode = result.user.PinYinCode ?? detail.PinYinCode ?? string.Empty;
                detail.PhoneNumber = result.user.PhoneNumber;
                detail.Email = result.user.Email;
                detail.Role = result.user.Role;
                detail.Status = result.user.Status;
                detail.CreatedAt = result.user.CreatedAt;
                detail.UpdatedAt = result.user.UpdatedAt;
                detail.Remark = result.user.Remark;

                Logger.LogInformation("用户{Action}成功: {UserId} - {UserName}",
                    IsNew ? "创建" : "更新", result.user.Id, result.user.UserName);

                OnPropertyChanged(nameof(DetailTitle));
                return true;
            }

            var errorMessage = !string.IsNullOrEmpty(result.errorMessage)
                ? result.errorMessage
                : (IsNew ? "创建用户失败" : "更新用户失败");
            Services.ErrorHandler.SetError("Save", errorMessage);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存用户失败: {UserName}", detail.UserName);
            var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage(
                IsNew ? "创建用户" : "更新用户", ex);
            Services.ErrorHandler.SetError("Save", errorMessage);
            return false;
        }
    }

    /// <summary>删除项</summary>
    protected override async Task<bool> DeleteItemAsync(UserListDto item)
    {
        // 检查是否删除当前登录用户
        var currentUser = _sessionManager?.CurrentUser;
        if (currentUser != null && item.Id == currentUser.Id)
        {
            await Services.Dialog.ShowWarningAsync("不能删除当前登录用户", "操作失败");
            return false;
        }

        var result = await _commandHandler.DeleteAsync(item.Id);
        if (!result.success)
        {
            Services.ErrorHandler.SetError("Delete", result.errorMessage ?? $"删除用户 '{item.UserName}' 失败");
        }
        else
        {
            Logger.LogInformation("用户删除成功: {UserId} - {UserName}", item.Id, item.UserName);
        }
        return result.success;
    }

    #endregion

    #region 筛选命令

    /// <summary>清除筛选</summary>
    [RelayCommand(CanExecute = nameof(CanClearFilters))]
    private void ClearFilters()
    {
        SelectedRoleFilter = null;
        SelectedStatusFilter = null;
        ShowInactiveUsers = false;
        SearchText = string.Empty;
    }

    private bool CanClearFilters() => HasActiveFilters;

    #endregion

    #region 扩展命令

    /// <summary>重置密码</summary>
    [RelayCommand(CanExecute = nameof(CanResetPassword))]
    private async Task ResetPasswordAsync()
    {
        if (SelectedItem == null) return;

        try
        {
            var user = SelectedItem;
            var confirmed = await Services.Dialog.ShowConfirmAsync(
                $"确认重置用户 [{user.RealName ?? user.UserName}] 的密码吗？\n\n密码将被重置为系统配置的默认密码",
                "重置密码确认");
            if (!confirmed) return;

            var result = await _commandHandler.ResetPasswordAsync(user.Id, null!);
            if (result.success && result.response != null)
            {
                await Services.Dialog.ShowSuccessAsync(
                    $"用户 [{user.RealName ?? user.UserName}] 的密码已重置\n\n新密码：{result.response.TemporaryPassword}",
                    "重置成功");
            }
            else
            {
                await Services.Dialog.ShowErrorAsync(result.errorMessage ?? "重置密码失败", "操作失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "重置密码失败");
            await Services.Dialog.ShowErrorAsync("重置密码失败", "操作失败");
        }
    }

    private bool CanResetPassword() => HasSelection && !IsBusy && SelectedItem?.Status == CommonStatus.Enabled;

    /// <summary>切换用户状态</summary>
    [RelayCommand(CanExecute = nameof(CanToggleUserStatus))]
    private async Task ToggleUserStatusAsync()
    {
        if (SelectedItem == null) return;

        try
        {
            var user = SelectedItem;
            var action = user.Status == CommonStatus.Enabled ? "禁用" : "启用";

            var result = await _commandHandler.ToggleStatusAsync(user.Id);
            if (result.success)
            {
                Logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName);
                await RefreshAsync();
            }
            else
            {
                await Services.Dialog.ShowErrorAsync(result.errorMessage ?? "切换用户状态失败", "操作失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "切换用户状态失败");
            await Services.Dialog.ShowErrorAsync("切换用户状态失败", "操作失败");
        }
    }

    private bool CanToggleUserStatus() => HasSelection && !IsBusy;

    /// <summary>查看审计日志</summary>
    [RelayCommand(CanExecute = nameof(CanShowAuditLog))]
    private void ShowAuditLog()
    {
        if (SelectedItem == null) return;

        Logger.LogInformation("查看用户审计日志：{UserId}", SelectedItem.Id);
        _prismDialogService.ShowDialog("EntityAuditLogDialog",
            new DialogParameters
            {
                { "EntityType", "user" },
                { "EntityId", SelectedItem.Id },
                { "EntityDescription", $"用户：{SelectedItem.RealName ?? SelectedItem.UserName}" }
            },
            _ => { });
    }

    private bool CanShowAuditLog() => HasSelection;

    /// <summary>恢复软删除</summary>
    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreAsync()
    {
        if (SelectedItem == null) return;

        try
        {
            var user = SelectedItem;
            var confirmed = await Services.Dialog.ShowConfirmAsync(
                $"确认恢复用户 [{user.RealName ?? user.UserName}] 吗？", "恢复确认");
            if (!confirmed) return;

            var result = await _userRepository.RestoreAsync(user.Id);
            if (result != null)
            {
                Logger.LogInformation("用户已恢复: {UserName}", user.UserName);
                await Services.Dialog.ShowSuccessAsync($"用户 '{user.RealName ?? user.UserName}' 已恢复", "操作成功");
                await RefreshAsync();
            }
            else
            {
                await Services.Dialog.ShowErrorAsync("恢复用户失败", "操作失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "恢复用户失败");
            await Services.Dialog.ShowErrorAsync("恢复用户失败", "操作失败");
        }
    }

    private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

    #endregion

    #region 导入导出命令

    /// <summary>导入用户</summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        if (_commonDialogService == null) return;

        try
        {
            var filePath = await _commonDialogService.ShowOpenFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "选择用户导入文件");
            if (string.IsNullOrEmpty(filePath)) return;

            using var fileStream = File.OpenRead(filePath);
            var users = await ExcelHelper.ParseAsync<UserInputDto>(fileStream, hasHeader: true);
            if (users == null || users.Count == 0)
            {
                await _commonDialogService.ShowErrorAsync("文件中没有有效的用户数据", "导入用户");
                return;
            }

            var request = new UserBatchImportInputDto
            {
                Users = users,
                Strategy = DuplicateStrategy.Skip
            };
            var result = await _userRepository.BatchImportAsync(request);
            if (result == null)
            {
                await _commonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入用户");
                return;
            }

            var message = $"导入完成！\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n成功率：{result.SuccessRate:F1}%";
            if (result.FailureCount > 0)
            {
                message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：";
                foreach (var f in result.Failures.Take(3))
                    message += $"\n第{f.OriginalRowNumber}行 [{f.UserName}]：{f.FailureReason}";
            }
            await _commonDialogService.ShowInfoAsync(message, "导入结果");
            if (result.SuccessCount > 0) await RefreshAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导入用户失败");
            await Services.Dialog.ShowErrorAsync("导入用户失败", "操作失败");
        }
    }

    /// <summary>导出用户</summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_commonDialogService == null) return;

        try
        {
            var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "导出用户数据",
                defaultFileName: $"用户数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            var allUsers = await _userRepository.SearchAsync(SearchText ?? string.Empty);
            if (allUsers == null || allUsers.Count == 0)
            {
                await _commonDialogService.ShowErrorAsync("没有可导出的数据", "导出用户");
                return;
            }

            await ExcelHelper.ExportAsync(allUsers, filePath, "用户数据");
            await _commonDialogService.ShowInfoAsync($"成功导出{allUsers.Count}条用户数据到：\n{filePath}", "导出成功");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导出用户失败");
            await Services.Dialog.ShowErrorAsync("导出用户失败", "操作失败");
        }
    }

    /// <summary>下载模板</summary>
    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        if (_commonDialogService == null) return;

        try
        {
            var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "保存用户导入模板",
                defaultFileName: $"用户导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            var sampleData = new List<UserInputDto>
            {
                new() { UserName = "doctor001", RealName = "张医生", PhoneNumber = "13800138000", Email = "doctor001@example.com", Role = UserRole.Doctor },
                new() { UserName = "admin001", RealName = "李管理员", PhoneNumber = "13800138001", Email = "admin001@example.com", Role = UserRole.Admin }
            };

            await ExcelHelper.GenerateTemplateAsync(filePath, "用户导入模板", sampleData);
            await _commonDialogService.ShowInfoAsync(
                $"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入用户」功能导入。\n\n注意：\n1. 用户名必须唯一\n2. 角色可选值：Admin(管理员)、Doctor(医生)、Nurse(护士)\n3. 新创建用户默认为启用状态",
                "下载成功");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "下载模板失败");
            await Services.Dialog.ShowErrorAsync("下载模板失败", "操作失败");
        }
    }

    #endregion
}
