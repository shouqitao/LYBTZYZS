using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Navigation;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Users.Models;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.ViewModels;

/// <summary>
/// 用户管理视图模型 - 基于ModernManagementViewModel
/// 使用UserItem作为UI模型，替代直接使用UserDto
/// 保持原有XAML绑定兼容性，确保功能不变
/// </summary>
public class UserManagementViewModel2 : ModernManagementViewModel<UserItem>
{
    #region Fields

    private readonly IUserService _userService;
    private readonly ICustomDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserManagementViewModel2> _logger;

    private string _roleFilter = "All";
    private string _statusFilter = "All";

    #endregion

    #region Properties

    /// <summary>
    /// 选中的用户 - 兼容原有绑定
    /// </summary>
    public UserItem? SelectedUser
    {
        get => SelectedItem;
        set => SelectedItem = value;
    }

    /// <summary>
    /// 角色筛选
    /// </summary>
    public string RoleFilter
    {
        get => _roleFilter;
        set
        {
            if (SetProperty(ref _roleFilter, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    /// <summary>
    /// 状态筛选
    /// </summary>
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    /// <summary>
    /// 重置密码命令
    /// </summary>
    public DelegateCommand ResetPasswordCommand { get; }

    /// <summary>
    /// 切换状态命令
    /// </summary>
    public DelegateCommand ToggleStatusCommand { get; }

    /// <summary>
    /// 分配权限命令
    /// </summary>
    public DelegateCommand AssignPermissionsCommand { get; }

    #endregion

    #region Constructor

    public UserManagementViewModel2(
        IUserService userService,
        ICustomDialogService dialogService,
        INavigationService navigationService,
        IEventAggregator eventAggregator,
        IMapper mapper,
        ILogger<UserManagementViewModel2> logger)
        : base(eventAggregator, dialogService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化额外命令
        ResetPasswordCommand = new DelegateCommand(
            async () => await ResetPasswordAsync(),
            () => CanResetPassword());

        ToggleStatusCommand = new DelegateCommand(
            async () => await ToggleStatusAsync(),
            () => CanToggleStatus());

        AssignPermissionsCommand = new DelegateCommand(
            async () => await AssignPermissionsAsync(),
            () => CanAssignPermissions());
    }

    #endregion

    #region Command Methods Override

    /// <summary>
    /// 加载数据实现
    /// </summary>
    protected override async Task LoadDataAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var searchDto = new UserSearchDto
            {
                Keyword = SearchKeyword,
                Role = ParseRoleFilter(),
                Status = ParseStatusFilter(),
                PageNumber = CurrentPage,
                PageSize = PageSize
            };

            var result = await _userService.GetPagedAsync(searchDto);

            if (result.IsSuccess && result.Data != null)
            {
                // 转换DTO到UI模型
                Items.Clear();
                foreach (var dto in result.Data.Items)
                {
                    Items.Add(UserItem.FromDto(dto));
                }

                TotalCount = result.Data.TotalCount;
            }
            else
            {
                await ShowErrorAsync(result.ErrorMessage ?? "加载用户数据失败");
            }
        });
    }

    /// <summary>
    /// 搜索实现
    /// </summary>
    protected override async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>
    /// 添加实现 - 创建新用户
    /// </summary>
    protected override async Task AddAsync()
    {
        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Mode", "Create" }
        };

        await _dialogService.ShowDialogAsync("UserCreateDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("用户创建成功");
            }
        });
    }

    /// <summary>
    /// 编辑实现
    /// </summary>
    protected override async Task EditAsync()
    {
        if (SelectedItem == null) return;

        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Mode", "Edit" },
            { "UserId", SelectedItem.Id }
        };

        await _dialogService.ShowDialogAsync("UserEditDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("用户更新成功");
            }
        });
    }

    /// <summary>
    /// 删除实现（实际是禁用）
    /// </summary>
    protected override async Task DeleteAsync()
    {
        if (SelectedItem == null) return;

        var action = SelectedItem.IsActive ? "禁用" : "启用";
        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"确定要{action}用户 {SelectedItem.Username} 吗？",
            $"确认{action}");

        if (confirmed)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = SelectedItem.IsActive
                    ? await _userService.DisableAsync(SelectedItem.Id)
                    : await _userService.EnableAsync(SelectedItem.Id);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync($"用户{action}成功");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? $"{action}失败");
                }
            });
        }
    }

    /// <summary>
    /// 查看详情实现
    /// </summary>
    protected override async Task ViewDetailsAsync()
    {
        if (SelectedItem == null) return;

        // 使用NavigationService导航到详情页
        var parameters = new NavigationParameters
        {
            { "UserId", SelectedItem.Id }
        };

        await _navigationService.NavigateToAsync(
            RegionNames.SystemWorkbenchContentRegion,
            "UserDetailView",
            parameters);
    }

    #endregion

    #region Additional Methods

    /// <summary>
    /// 重置密码
    /// </summary>
    private async Task ResetPasswordAsync()
    {
        if (SelectedItem == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"确定要重置用户 {SelectedItem.Username} 的密码吗？\n密码将重置为默认密码。",
            "确认重置密码");

        if (confirmed)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _userService.ResetPasswordAsync(
                    SelectedItem.Id,
                    string.Empty); // 后端会自动使用默认密码

                if (result.IsSuccess)
                {
                    await ShowSuccessAsync("密码重置成功，请通知用户使用默认密码登录");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "密码重置失败");
                }
            });
        }
    }

    /// <summary>
    /// 切换用户状态
    /// </summary>
    private async Task ToggleStatusAsync()
    {
        if (SelectedItem == null) return;

        await DeleteAsync(); // 复用删除逻辑（实际是状态切换）
    }

    /// <summary>
    /// 分配权限
    /// </summary>
    private async Task AssignPermissionsAsync()
    {
        if (SelectedItem == null) return;

        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "UserId", SelectedItem.Id },
            { "Username", SelectedItem.Username },
            { "CurrentRole", SelectedItem.Role }
        };

        await _dialogService.ShowDialogAsync("PermissionAssignDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("权限分配成功");
            }
        });
    }

    /// <summary>
    /// 解析角色筛选
    /// </summary>
    private UserRole? ParseRoleFilter()
    {
        return RoleFilter switch
        {
            "Doctor" => UserRole.Doctor,
            "Admin" => UserRole.Admin,
            _ => null
        };
    }

    /// <summary>
    /// 解析状态筛选
    /// </summary>
    private CommonStatus? ParseStatusFilter()
    {
        return StatusFilter switch
        {
            "Enabled" => CommonStatus.Enabled,
            "Disabled" => CommonStatus.Disabled,
            _ => null
        };
    }

    /// <summary>
    /// 是否可以重置密码
    /// </summary>
    private bool CanResetPassword()
    {
        return SelectedItem != null && SelectedItem.IsActive;
    }

    /// <summary>
    /// 是否可以切换状态
    /// </summary>
    private bool CanToggleStatus()
    {
        return SelectedItem != null;
    }

    /// <summary>
    /// 是否可以分配权限
    /// </summary>
    private bool CanAssignPermissions()
    {
        return SelectedItem != null && SelectedItem.IsActive;
    }

    /// <summary>
    /// 选中项变化处理
    /// </summary>
    protected override void OnSelectedItemChanged(UserItem? newItem)
    {
        base.OnSelectedItemChanged(newItem);

        // 更新命令状态
        ResetPasswordCommand.RaiseCanExecuteChanged();
        ToggleStatusCommand.RaiseCanExecuteChanged();
        AssignPermissionsCommand.RaiseCanExecuteChanged();
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// 初始化
    /// </summary>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await LoadDataAsync();
    }

    #endregion
}