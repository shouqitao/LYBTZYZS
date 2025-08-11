using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Core;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Users;

namespace LYBT.Desktop.Shared.ViewModels.Users
{
    /// <summary>
    /// 用户管理视图模型（简化重构版�?    /// </summary>
    public class UserManagementViewModelSimple : BaseServiceManagementViewModel<UserInfo, IUserService>
    {
        private readonly IDialogService _commonDialogService;
        private readonly IDialogService _dialogService;
        private readonly IUserApiService _userApiService;

        protected override string ModuleName => "用户管理";

        #region Commands

        public DelegateCommand<UserInfo> ResetPasswordCommand { get; }
        public DelegateCommand<UserInfo> ToggleStatusCommand { get; }

        #endregion

        public UserManagementViewModelSimple(
            IUserService userService,
            IUserApiService userApiService,
            IDialogService commonDialogService,
            IDialogService dialogService,
            Prism.Events.IEventAggregator eventAggregator)
            : base(userService, eventAggregator)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            _userApiService = userApiService;

            // 初始化命�?            ResetPasswordCommand = new DelegateCommand<UserInfo>(async user => await ResetPasswordAsync(user));
            ToggleStatusCommand = new DelegateCommand<UserInfo>(async user => await ToggleStatusAsync(user));
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<PagedResult<UserInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                var query = new UserPagedQueryDto
                {
                    PageIndex = request.CurrentPage,
                    PageSize = request.PageSize,
                    Keyword = SearchKeyword
                };

                var result = await Service.SearchUsersAsync(query);
                return ServiceResult<PagedResult<UserInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserInfo>>.Failure($"加载用户列表失败: {ex.Message}");
            }
        }

        protected override async Task AddAsync()
        {
            try
            {
                var dialog = new Views.Users.UserAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "新增用户";

                // 创建ViewModel并设置为添加模式
                var viewModel = new UserAddEditDialogViewModel(_userApiService, null); // null表示新增
                dialog.DataContext = viewModel;

                // 设置保存成功回调
                viewModel.SaveCompleteCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                };

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("用户添加成功", "成功");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"添加用户失败: {ex.Message}", "错误");
            }
        }

        protected override async Task EditAsync(UserInfo item)
        {
            if (item == null) return;

            try
            {
                var dialog = new Views.Users.UserAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "编辑用户";

                // 创建ViewModel并设置为编辑模式
                var viewModel = new UserAddEditDialogViewModel(_userApiService, item);
                dialog.DataContext = viewModel;

                // 设置保存成功回调
                viewModel.SaveCompleteCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                };

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("用户编辑成功", "成功");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"编辑用户失败: {ex.Message}", "错误");
            }
        }

        protected override async Task DeleteAsync(UserInfo item)
        {
            if (item == null) return;

            // 不允许删除系统管理员账号
            if (item.Username == "admin" || item.Username == "sysadmin")
            {
                await _commonDialogService.ShowWarningAsync("不允许删除系统管理员账号", "警告");
                return;
            }

            // 用户不支持删除，只能禁用
            await ToggleStatusAsync(item);
        }

        #endregion

        #region 额外方法

        /// <summary>
        /// 重置密码
        /// </summary>
        private async Task ResetPasswordAsync(UserInfo user)
        {
            if (user == null) return;

            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要重置用户 {user.RealName} 的密码吗？",
                "重置密码");

            if (confirm)
            {
                var result = await Service.ResetPasswordAsync(user.Id);
                if (result.IsSuccess)
                {
                    await _commonDialogService.ShowInformationAsync("密码重置成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "密码重置失败",
                        "错误");
                }
            }
        }

        /// <summary>
        /// 切换用户状�?        /// </summary>
        private async Task ToggleStatusAsync(UserInfo user)
        {
            if (user == null) return;

            var action = user.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled ? "禁用" : "启用";
            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要{action}用户 {user.RealName} 吗？",
                $"{action}用户");

            if (confirm)
            {
                ServiceResult result;
                if (user.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled)
                {
                    result = await Service.DisableUserAsync(user.Id);
                }
                else
                {
                    result = await Service.EnableUserAsync(user.Id);
                }

                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync($"用户{action}成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"用户{action}失败",
                        "错误");
                }
            }
        }

        #endregion
    }
}
