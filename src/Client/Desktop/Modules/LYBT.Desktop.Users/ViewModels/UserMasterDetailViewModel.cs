using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Users.Mappers;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Handlers;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Utilities.Text;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.ViewModels;

/// <summary>
/// 用户Master-Detail视图模型（组合模式）
///
/// 使用IMasterDetailServices实现组合模式
/// </summary>
public partial class UserMasterDetailViewModel : MasterDetailViewModelBase<UserListDto, UserDetailModel>
{
    private readonly IUserService _commandHandler;
    private readonly IUserPasswordHandler _passwordHandler;
    private readonly IUserStatusHandler _statusHandler;
    private readonly IDesktopCacheManager _cacheManager;
    private readonly UserMapper _userMapper;

    /// <summary>用户编辑子 VM</summary>
    public UserEditorViewModel UserEditor { get; }

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

    /// <inheritdoc/>
    protected override string EntityDisplayName => "用户";

    /// <inheritdoc/>
    protected override string NewEntityVerb => "新增";

    /// <inheritdoc/>
    protected override string? GetDetailDisplayName() => CurrentDetail?.RealName;

    /// <summary>用户名是否只读（编辑模式下不可修改）</summary>
    public bool IsUserNameReadOnly => CurrentDetail != null && !IsNew;

    /// <summary>角色选项</summary>
    public ObservableCollection<UserRole> RoleOptions { get; } = new(Enum.GetValues<UserRole>());

    /// <summary>状态选项</summary>
    public ObservableCollection<CommonStatus> StatusOptions { get; } = new(CommonOptions.StatusOptions);

    #endregion

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserMasterDetailViewModel(
        IViewModelServices viewModelServices,
        IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices,
        IUserService commandHandler,
        IUserPasswordHandler passwordHandler,
        IUserStatusHandler statusHandler,
        IDesktopCacheManager cacheManager,
        UserMapper userMapper,
        UserEditorViewModel userEditor)
        : base(viewModelServices, masterDetailServices)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
        _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
        UserEditor = userEditor ?? throw new ArgumentNullException(nameof(userEditor));

        PageTitle = "用户管理";

        // 监听属性变化 - DetailTitle 已由基类自动通知
        PropertyChanged += OnDetailPropertyChanged;
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
                if (result.Success && result.Data != null)
                {
                    MasterDetailServices.Pagination.TotalCount = result.Data.TotalCount;

                    Items.Clear();
                    var filteredItems = ApplyFilters(result.Data.Items);
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
            if (!result.Success || result.Data == null)
            {
                await MasterDetailServices.Dialog.ShowErrorAsync($"用户 '{item.UserName}' 不存在或已被删除", "加载失败");
                return;
            }

            // D2: 改用 Mapperly UserMapper，消除 DetailModel + EditContext 双份手写映射
            UserEditor.InitializeFromDto(result.Data);
            var detail = _userMapper.ToDetailModel(result.Data);

            MasterDetailServices.DetailEditor.LoadDetail(detail);
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
        UserEditor.InitializeForNewCase();
        var detail = UserDetailModel.CreateNew();
        OnPropertyChanged(nameof(IsUserNameReadOnly));
        return detail;
    }

    /// <summary>保存详情</summary>
    protected override async Task<bool> SaveDetailAsync(UserDetailModel detail)
    {
        if (!UserEditor.Validate())
        {
            await MasterDetailServices.Dialog.ShowErrorAsync("请修正验证错误后重试", "验证失败");
            return false;
        }

        try
        {
            var input = UserEditor.GetUserInput();

            var result = UserEditor.User.Id == Guid.Empty
                ? await _commandHandler.CreateAsync(input)
                : await _commandHandler.UpdateAsync(input);

            if (result.Success && result.Data != null)
            {
                // D2: 改用 Mapperly ApplyToDetailModel 回填（保留 PinYinCode 原值当返回为空）
                _userMapper.ApplyToDetailModel(detail, result.Data);

                Logger.LogInformation("用户{Action}成功: {UserId} - {UserName}",
                    UserEditor.User.Id == Guid.Empty ? "创建" : "更新", result.Data.Id, result.Data.UserName);

                _cacheManager.InvalidateUserCaches();
                return true;
            }

            var errorMessage = !string.IsNullOrEmpty(result.Error)
                ? result.Error
                : (UserEditor.User.Id == Guid.Empty ? "创建用户失败" : "更新用户失败");
            MasterDetailServices.ErrorHandler.SetError("Save", errorMessage);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存用户失败: {UserName}", detail.UserName);
            var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage(
                UserEditor.User.Id == Guid.Empty ? "创建用户" : "更新用户", ex);
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
        if (!result.Success)
        {
            MasterDetailServices.ErrorHandler.SetError("Delete", result.Error ?? $"删除用户 '{item.UserName}' 失败");
        }
        else
        {
            Logger.LogInformation("用户删除成功: {UserId} - {UserName}", item.Id, item.UserName);
            _cacheManager.InvalidateUserCaches();
        }
        return result.Success;
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

    /// <inheritdoc/>
    protected override async Task InvalidateCachesAsync()
    {
        _cacheManager.InvalidateUserCaches();
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task RestoreItemAsync(UserListDto item)
    {
        await _statusHandler.RestoreAsync(item);
    }

    /// <summary>批量删除（单次 batch-delete 调用，替代逐条删除）</summary>
    protected override async Task DeleteBatchAsync(List<UserListDto> items)
    {
        // 与单删一致：不允许删除当前登录用户
        var currentUser = SessionManager?.CurrentUser;
        var deletable = items.Where(u => currentUser == null || u.Id != currentUser.Id).ToList();
        if (deletable.Count < items.Count)
        {
            await MasterDetailServices.Dialog.ShowWarningAsync("已跳过当前登录用户", "操作提示");
        }

        if (deletable.Count == 0) return;

        var result = await _commandHandler.BatchDeleteAsync(deletable.Select(u => u.Id).ToList());
        if (result.Success && result.Data != null)
        {
            Logger.LogInformation("用户批量删除完成: 成功 {Success}, 失败 {Failure}",
                result.Data.SuccessCount, result.Data.FailureCount);
            _cacheManager.InvalidateUserCaches();

            // P1-C：部分失败需提示用户
            if (result.Data.FailureCount > 0)
            {
                await MasterDetailServices.Dialog.ShowWarningAsync(
                    $"批量删除完成：成功 {result.Data.SuccessCount} 条，失败 {result.Data.FailureCount} 条", "部分失败");
            }
        }
        else
        {
            MasterDetailServices.ErrorHandler.SetError("BatchDelete", result.Error ?? "批量删除用户失败");
        }
    }

    /// <summary>批量启用（单次 batch-enable 调用）</summary>
    protected override async Task EnableBatchAsync(List<UserListDto> items)
        => await BatchSetStatusAsync(items, CommonStatus.Enabled, "批量启用");

    /// <summary>批量禁用（单次 batch-disable 调用）</summary>
    protected override async Task DisableBatchAsync(List<UserListDto> items)
        => await BatchSetStatusAsync(items, CommonStatus.Disabled, "批量禁用");

    private async Task BatchSetStatusAsync(List<UserListDto> items, CommonStatus status, string operationName)
    {
        var result = await _commandHandler.BatchSetStatusAsync(items.Select(u => u.Id).ToList(), status);
        if (result.Success && result.Data != null)
        {
            Logger.LogInformation("{Operation}完成: 成功 {Success}, 失败 {Failure}",
                operationName, result.Data.SuccessCount, result.Data.FailureCount);
            _cacheManager.InvalidateUserCaches();

            // P1-C：部分失败需提示用户
            if (result.Data.FailureCount > 0)
            {
                await MasterDetailServices.Dialog.ShowWarningAsync(
                    $"{operationName}完成：成功 {result.Data.SuccessCount} 条，失败 {result.Data.FailureCount} 条", "部分失败");
            }
        }
        else
        {
            MasterDetailServices.ErrorHandler.SetError(operationName, result.Error ?? $"{operationName}用户失败");
        }
    }

    #endregion

    #region Disposal

    private void OnDetailPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CurrentDetail) or nameof(IsEditMode))
        {
            OnPropertyChanged(nameof(IsUserNameReadOnly));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PropertyChanged -= OnDetailPropertyChanged;
        }
        base.Dispose(disposing);
    }

    #endregion
}
