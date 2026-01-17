using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 账户设置视图模型 - 合并个人资料和修改密码功能
    /// OpenSpec: migrate-views-to-role-modules - 从Users模块迁移到Shell
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CoreViewModelBase
    /// </summary>
    public partial class AccountSettingsViewModel : CoreViewModelBase, INavigationAware
    {
        #region 依赖服务

        private readonly IAuthenticationService _authService;
        private readonly IUserRepository _userRepository;
        private readonly ISessionManager _sessionManager;
        private readonly IRegionManager _regionManager;
        private readonly IUserNotificationService? _userNotificationService;

        #endregion

        #region 个人资料属性

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _realName = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _role = string.Empty;

        #endregion

        #region 修改密码属性

        [ObservableProperty]
        private string _oldPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        #endregion

        #region 验证属性

        [ObservableProperty]
        private string _validationError = string.Empty;

        [ObservableProperty]
        private bool _hasValidationError;

        #endregion

        #region Tab选择属性

        /// <summary>个人资料页是否选中</summary>
        [ObservableProperty]
        private bool _isProfileSelected = true;

        /// <summary>修改密码页是否选中</summary>
        [ObservableProperty]
        private bool _isPasswordSelected;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
        /// </summary>
        public AccountSettingsViewModel(
            IViewModelServices services,
            IAuthenticationService authService,
            IUserRepository userRepository,
            IUserNotificationService? userNotificationService = null)
            : base(services)
        {
            _authService = authService;
            _userRepository = userRepository;
            _sessionManager = services.SessionManager;
            _regionManager = services.RegionManager;
            _userNotificationService = userNotificationService;

            LoadUserProfile();
        }

        #endregion

        #region 命令

        /// <summary>
        /// 保存个人资料
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveProfile))]
        private async Task SaveProfileAsync()
        {
            try
            {
                IsBusy = true;
                ClearValidationError();

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(RealName))
                {
                    SetValidationError("姓名不能为空");
                    return;
                }

                var currentUser = _sessionManager.CurrentUser;
                if (currentUser == null)
                {
                    SetValidationError("未找到当前用户信息");
                    return;
                }

                // 构造修改资料DTO
                var profileDto = new ChangeProfileDto
                {
                    RealName = RealName,
                    PhoneNumber = PhoneNumber
                };

                // 调用Repository更新用户信息
                var updatedUser = await _userRepository.ChangeProfileAsync(currentUser.Id, profileDto);

                if (updatedUser != null)
                {
                    Logger.LogInformation("用户资料更新成功: {UserName}", UserName);
                    await NotifySuccessAsync("个人资料已保存");

                    // 刷新本地显示（不需要更新Session，下次登录会自动刷新）
                    LoadUserProfileFromDto(updatedUser);
                }
                else
                {
                    SetValidationError("保存失败，请稍后重试");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存个人资料失败");
                SetValidationError($"保存失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSaveProfile() => !IsBusy && !string.IsNullOrWhiteSpace(RealName);

        /// <summary>
        /// 修改密码
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangePassword))]
        private async Task ChangePasswordAsync()
        {
            try
            {
                IsBusy = true;
                ClearValidationError();

                // 验证密码
                if (!ValidatePasswordChange())
                {
                    return;
                }

                var currentUser = _sessionManager.CurrentUser;
                if (currentUser == null)
                {
                    SetValidationError("未找到当前用户信息");
                    return;
                }

                // Issue #2262: 改用IUserRepository统一密码修改逻辑
                // 职责分离：Auth负责认证，User负责用户管理（包括密码修改）
                var request = new ChangePasswordRequest
                {
                    OldPassword = OldPassword,
                    NewPassword = NewPassword
                };
                var result = await _userRepository.ChangePasswordAsync(currentUser.Id, request);

                if (result.IsSuccess)
                {
                    Logger.LogInformation("密码修改成功: {UserName}", UserName);
                    await NotifySuccessAsync("密码修改成功");

                    // 清空密码字段
                    ClearPasswordFields();
                }
                else
                {
                    SetValidationError(result.ErrorMessage ?? "密码修改失败，请检查当前密码是否正确");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "修改密码失败");
                SetValidationError($"修改失败: {ex.Message}");
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

        /// <summary>
        /// 返回上一页
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            var journal = _regionManager.Regions[RegionNames.ContentRegion].NavigationService?.Journal;
            if (journal?.CanGoBack == true)
            {
                journal.GoBack();
            }
        }

        #endregion

        #region 私有方法

        private void LoadUserProfile()
        {
            var currentUser = _sessionManager.CurrentUser;
            if (currentUser != null)
            {
                LoadUserProfileFromDto(currentUser);
            }
        }

        private void LoadUserProfileFromDto(UserDetailDto user)
        {
            UserName = user.UserName;
            RealName = user.RealName ?? string.Empty;
            PhoneNumber = user.PhoneNumber ?? string.Empty;
            Role = GetRoleDisplayName(user.Role);
        }

        private static string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.SuperAdmin => "超级管理员",
                UserRole.Admin => "管理员",
                UserRole.Doctor => "医生",
                UserRole.Receptionist => "前台接待",
                _ => role.ToString()
            };
        }

        private async Task NotifySuccessAsync(string message)
        {
            if (_userNotificationService != null)
            {
                await _userNotificationService.ShowSuccessAsync(message);
            }
        }

        private bool ValidatePasswordChange()
        {
            if (string.IsNullOrWhiteSpace(OldPassword))
            {
                SetValidationError("请输入当前密码");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                SetValidationError("请输入新密码");
                return false;
            }

            if (NewPassword.Length < 6)
            {
                SetValidationError("新密码长度不能少于6位");
                return false;
            }

            if (NewPassword != ConfirmPassword)
            {
                SetValidationError("两次输入的密码不一致");
                return false;
            }

            if (OldPassword == NewPassword)
            {
                SetValidationError("新密码不能与当前密码相同");
                return false;
            }

            return true;
        }

        private void ClearPasswordFields()
        {
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private void SetValidationError(string error)
        {
            ValidationError = error;
            HasValidationError = true;
        }

        private void ClearValidationError()
        {
            ValidationError = string.Empty;
            HasValidationError = false;
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 检查是否指定了默认Tab
            if (navigationContext.Parameters.ContainsKey("Tab"))
            {
                var tab = navigationContext.Parameters.GetValue<string>("Tab");
                IsPasswordSelected = tab == "Password";
                IsProfileSelected = !IsPasswordSelected;
            }
            else
            {
                // 默认显示个人资料
                IsProfileSelected = true;
                IsPasswordSelected = false;
            }

            LoadUserProfile();
            ClearValidationError();
            ClearPasswordFields();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 清理敏感数据
            ClearPasswordFields();
        }

        #endregion
    }
}
