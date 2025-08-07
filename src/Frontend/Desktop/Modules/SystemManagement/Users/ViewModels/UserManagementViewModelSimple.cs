using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Core;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Commands;

using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Modules.SystemManagement.Users.ViewModels
{
    /// <summary>
    /// 用户管理视图模型（简化重构版）
    /// </summary>
    public class UserManagementViewModelSimple : BaseManagementViewModel<UserInfo, IUserApiService>
    {
        private readonly ICommonDialogService _commonDialogService;

        protected override string ModuleName => "用户管理";

        public UserManagementViewModelSimple(IUserApiService service,
            ICommonDialogService commonDialogService)
            : base(service)
        {
            _commonDialogService = commonDialogService;
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<PagedResult<UserInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                var query = new UserPagedQueryDto
                {
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize,
                    SearchKeyword = SearchKeyword
                };

                var response = await Service.GetPagedUsersAsync(query);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var paginatedResult = response.Content;
                    
                    // 转换为前端模型
                    var userInfos = paginatedResult.Items.Select(dto => new UserInfo { RealName = dto.RealName ?? string.Empty,
                        // Email字段已按优化标准移除,
                        PhoneNumber = dto.PhoneNumber,
                        // Role = dto.Username == "sysadmin" ? "管理员" : "用户", // Role字段已移除
                        /* Department = dto.Department, */
                        // IsActive字段已按优化标准移除
                        CreateTime = dto.CreateTime,
                        PinYinCode = dto.PinYinCode,
                        WuBiCode = dto.WuBiCode
                    }).ToList();

                    var result = new PagedResult<UserInfo>
                    {
                        Items = userInfos,
                        TotalCount = paginatedResult.TotalCount,
                        CurrentPage = paginatedResult.CurrentPage,
                        PageSize = paginatedResult.PageSize
                    };

                    return ServiceResult<PagedResult<UserInfo>>.Success(result);
                }
                else
                {
                    var error = response.Error?.Content ?? "获取用户列表失败";
                    return ServiceResult<PagedResult<UserInfo>>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserInfo>>.Failure($"加载用户列表失败: {ex.Message}");
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(UserInfo item)
        {
            // 用户不支持删除，只能禁用
            try
            {
                var response = await Service.DisableUserAsync(item.Id);
                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    var error = response.Error?.Content ?? "禁用用户失败";
                    return ServiceResult<bool>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"禁用用户失败: {ex.Message}");
            }
        }

        protected override string GetItemDisplayName(UserInfo item)
        {
            return $"{item.RealName}({item.Username})";
        }

        protected override bool CanExecuteDelete(UserInfo item)
        {
            // 不允许删除系统管理员账号
            return item != null && item.Username != "admin" && item.Username != "sysadmin";
        }

        protected override void ExecuteAdd()
        {
            try
            {
                var dialog = new Views.UserAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "新增用户";
                
                // 创建ViewModel并设置为添加模式
                var viewModel = new UserAddEditDialogViewModel(Service, null); // null表示新增
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
                    RefreshCommand.Execute();
                    _commonDialogService.ShowInformationAsync("用户添加成功", "成功").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"添加用户失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteEdit(UserInfo item)
        {
            if (item == null) return;

            try
            {
                var dialog = new Views.UserAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "编辑用户";
                
                // 创建ViewModel并设置为编辑模式
                var viewModel = new UserAddEditDialogViewModel(Service, item);
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
                    RefreshCommand.Execute();
                    _commonDialogService.ShowInformationAsync("用户编辑成功", "成功").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"编辑用户失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion
    }
}