using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// 账户设置视图模型 - 左右分栏重设计 (2026-06-21)
/// 个人资料 + 安全设置（修改密码）合并
/// </summary>
public partial class AccountSettingsViewModel : CoreViewModelBase, INavigationAware
{
    private readonly IAuthenticationService _authService;
    private readonly IApiClientUsers _userApi;
    private readonly INavigationCoordinator _navigationCoordinator;

    #region Tab 选择

    [ObservableProperty]
    private bool _isProfileSelected = true;

    [ObservableProperty]
    private bool _isPasswordSelected;

    #endregion

    #region 当前用户（只读显示）

    [ObservableProperty]
    private UserDetailDto? _currentUser;

    #endregion

    #region 可编辑字段

    [ObservableProperty]
    private string _editRealName = string.Empty;

    [ObservableProperty]
    private string _editPhoneNumber = string.Empty;

    [ObservableProperty]
    private string _editEmail = string.Empty;

    #endregion

    #region 密码字段

    [ObservableProperty]
    private string _oldPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    #endregion

    public AccountSettingsViewModel(
        IViewModelServices services,
        IAuthenticationService authService,
        IApiClientUsers userApi,
        INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _authService = authService;
        _userApi = userApi;
        _navigationCoordinator = navigationCoordinator;
    }

    #region 保存个人资料

    [RelayCommand(CanExecute = nameof(CanSaveProfile))]
    private async Task SaveProfileAsync()
    {
        if (CurrentUser == null)
        {
            Services.ToastService.ShowError("未找到当前用户信息");
            return;
        }

        if (string.IsNullOrWhiteSpace(EditRealName))
        {
            Services.ToastService.ShowWarning("姓名不能为空");
            return;
        }

        try
        {
            IsBusy = true;
            var dto = new ChangeProfileDto
            {
                RealName = EditRealName,
                PhoneNumber = string.IsNullOrWhiteSpace(EditPhoneNumber) ? null : EditPhoneNumber,
                Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail
            };

            var resp = await _userApi.ChangeProfileAsync(CurrentUser.Id, dto);
            if (resp.Success)
            {
                if (resp.Data != null)
                {
                    CurrentUser = resp.Data;
                    // 发布事件通知 MainWindowViewModel 同步
                    Services.EventAggregator.GetEvent<AuthEvents.ProfileUpdatedEvent>()
                        .Publish(new ProfileUpdatedPayload { UpdatedUser = resp.Data });
                }
                Services.ToastService.ShowSuccess("个人资料已保存");
                Logger.LogInformation("用户资料更新成功: {UserName}", CurrentUser.UserName);
            }
            else
            {
                Services.ToastService.ShowError(resp.Message ?? "保存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存个人资料失败");
            Services.ToastService.ShowError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存个人资料", ex));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSaveProfile() => !IsBusy && !string.IsNullOrWhiteSpace(EditRealName);

    #endregion

    #region 修改密码

    [RelayCommand(CanExecute = nameof(CanChangePassword))]
    private async Task ChangePasswordAsync()
    {
        if (CurrentUser == null)
        {
            Services.ToastService.ShowError("未找到当前用户信息");
            return;
        }

        if (string.IsNullOrWhiteSpace(OldPassword))
        {
            Services.ToastService.ShowWarning("请输入当前密码");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            Services.ToastService.ShowWarning("请输入新密码");
            return;
        }

        if (NewPassword.Length < 8)
        {
            Services.ToastService.ShowWarning("新密码长度不能少于8位");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            Services.ToastService.ShowWarning("两次输入的密码不一致");
            return;
        }

        if (OldPassword == NewPassword)
        {
            Services.ToastService.ShowWarning("新密码不能与当前密码相同");
            return;
        }

        try
        {
            IsBusy = true;
            var request = new ChangePasswordRequest
            {
                OldPassword = OldPassword,
                NewPassword = NewPassword
            };

            var resp = await _userApi.ChangePasswordAsync(CurrentUser.Id, request);
            if (resp.Success)
            {
                Services.ToastService.ShowSuccess("密码修改成功");
                Logger.LogInformation("密码修改成功: {UserName}", CurrentUser.UserName);
                ClearPasswordFields();
            }
            else
            {
                Services.ToastService.ShowError(resp.Message ?? "密码修改失败，请检查当前密码是否正确");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "修改密码失败");
            Services.ToastService.ShowError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanChangePassword() =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(OldPassword) &&
        !string.IsNullOrWhiteSpace(NewPassword) &&
        !string.IsNullOrWhiteSpace(ConfirmPassword);

    #endregion

    #region 返回

    [RelayCommand]
    private void GoBack() => _navigationCoordinator.NavigateBack();

    #endregion

    #region 加载用户资料

    private async Task LoadUserProfileAsync()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
            {
                CurrentUser = user;
                EditRealName = user.RealName ?? string.Empty;
                EditPhoneNumber = user.PhoneNumber ?? string.Empty;
                EditEmail = user.Email ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载用户资料失败");
        }
    }

    private void ClearPasswordFields()
    {
        OldPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    #endregion

    #region INavigationAware

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (navigationContext.Parameters.ContainsKey("Tab"))
        {
            var tab = navigationContext.Parameters.GetValue<string>("Tab");
            IsPasswordSelected = tab == "Password";
            IsProfileSelected = !IsPasswordSelected;
        }
        else
        {
            IsProfileSelected = true;
            IsPasswordSelected = false;
        }

        ClearPasswordFields();
        await LoadUserProfileAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        ClearPasswordFields();
    }

    #endregion
}
