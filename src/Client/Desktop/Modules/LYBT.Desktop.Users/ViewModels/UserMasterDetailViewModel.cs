using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Desktop.Users.ViewModels.Handlers;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

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
    private readonly IUserPasswordHandler _passwordHandler;
    private readonly IUserStatusHandler _statusHandler;
    private readonly IUserImportExportHandler _importExportHandler;
    private readonly IDesktopCacheManager _cacheManager;

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
                MasterDetailServices.Pagination.GoToFirstPage();
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
                MasterDetailServices.Pagination.GoToFirstPage();
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
                MasterDetailServices.Pagination.GoToFirstPage();
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
    public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

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

    /// <summary>
    /// 构造函数
    /// OpenSpec: refactor-frontend-srp-patterns - 使用Handler组件化模式
    /// </summary>
    public UserMasterDetailViewModel(
        IViewModelServices viewModelServices,
        IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices,
        UserService commandHandler,
        IUserPasswordHandler passwordHandler,
        IUserStatusHandler statusHandler,
        IUserImportExportHandler importExportHandler,
        IDesktopCacheManager cacheManager)
        : base(viewModelServices, masterDetailServices)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
        _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
        _importExportHandler = importExportHandler ?? throw new ArgumentNullException(nameof(importExportHandler));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));

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
            await MasterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
            {
                var result = await _commandHandler.GetPagedAsync(CurrentPage, PageSize, SearchText);
                if (result.success && result.data != null)
                {
                    MasterDetailServices.Pagination.TotalCount = result.data.TotalCount;

                    Items.Clear();
                    var filteredItems = ApplyFilters(result.data.Items);
                    foreach (var item in filteredItems)
                    {
                        Items.Add(item);
                    }
                }
                else
                {
                    MasterDetailServices.Pagination.TotalCount = 0;
                    Items.Clear();
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取用户列表时发生异常");
            MasterDetailServices.ErrorHandler.HandleException(ex, "获取用户列表");
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
                await MasterDetailServices.Dialog.ShowErrorAsync($"用户 '{item.UserName}' 不存在或已被删除", "加载失败");
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

            MasterDetailServices.DetailEditor.LoadDetail(detail);
            OnPropertyChanged(nameof(DetailTitle));
            OnPropertyChanged(nameof(IsUserNameReadOnly));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载用户详情失败: {UserId}", item.Id);
            MasterDetailServices.ErrorHandler.HandleException(ex, "加载用户详情");
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
            await MasterDetailServices.Dialog.ShowErrorAsync("用户名不能为空", "验证失败");
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

                _cacheManager.InvalidateUserCaches();
                OnPropertyChanged(nameof(DetailTitle));
                return true;
            }

            var errorMessage = !string.IsNullOrEmpty(result.errorMessage)
                ? result.errorMessage
                : (IsNew ? "创建用户失败" : "更新用户失败");
            MasterDetailServices.ErrorHandler.SetError("Save", errorMessage);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存用户失败: {UserName}", detail.UserName);
            var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage(
                IsNew ? "创建用户" : "更新用户", ex);
            MasterDetailServices.ErrorHandler.SetError("Save", errorMessage);
            return false;
        }
    }

    /// <summary>删除项</summary>
    protected override async Task<bool> DeleteItemAsync(UserListDto item)
    {
        // 检查是否删除当前登录用户
        var currentUser = SessionManager?.CurrentUser;
        if (currentUser != null && item.Id == currentUser.Id)
        {
            await MasterDetailServices.Dialog.ShowWarningAsync("不能删除当前登录用户", "操作失败");
            return false;
        }

        var result = await _commandHandler.DeleteAsync(item.Id);
        if (!result.success)
        {
            MasterDetailServices.ErrorHandler.SetError("Delete", result.errorMessage ?? $"删除用户 '{item.UserName}' 失败");
        }
        else
        {
            Logger.LogInformation("用户删除成功: {UserId} - {UserName}", item.Id, item.UserName);
            _cacheManager.InvalidateUserCaches();
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
        await _passwordHandler.ResetPasswordAsync(SelectedItem);
    }

    private bool CanResetPassword() => _passwordHandler.CanResetPassword(SelectedItem, IsBusy);

    /// <summary>切换用户状态</summary>
    [RelayCommand(CanExecute = nameof(CanToggleUserStatus))]
    private async Task ToggleUserStatusAsync()
    {
        if (SelectedItem == null) return;
        if (await _statusHandler.ToggleUserStatusAsync(SelectedItem))
        {
            _cacheManager.InvalidateUserCaches();
            await RefreshAsync();
        }
    }

    private bool CanToggleUserStatus() => _statusHandler.CanToggleUserStatus(SelectedItem, IsBusy);

    /// <summary>恢复软删除</summary>
    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreAsync()
    {
        if (SelectedItem == null) return;
        if (await _statusHandler.RestoreAsync(SelectedItem))
        {
            _cacheManager.InvalidateUserCaches();
            await RefreshAsync();
        }
    }

    private bool CanRestore() => _statusHandler.CanRestore(SelectedItem, IsBusy, IsAdmin);

    #endregion

    #region 导入导出命令

    /// <summary>导入用户</summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        if (await _importExportHandler.ImportAsync())
        {
            _cacheManager.InvalidateUserCaches();
            await RefreshAsync();
        }
    }

    /// <summary>导出用户</summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        await _importExportHandler.ExportAsync(SearchText);
    }

    /// <summary>下载模板</summary>
    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        await _importExportHandler.DownloadTemplateAsync();
    }

    #endregion
}
