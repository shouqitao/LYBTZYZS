using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Commands;

namespace LYBT.WPF.Client.Modules.SystemManagement.Users.ViewModels
{
    /// <summary>
    /// 用户管理视图模型（简化重构版）
    /// </summary>
    public class UserManagementViewModelSimple : BaseManagementViewModel<UserInfo, IUserApiService>
    {
        protected override string ModuleName => "用户管理";

        public UserManagementViewModelSimple(IUserApiService service)
            : base(service)
        {
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
                    var userInfos = paginatedResult.Items.Select(dto => new UserInfo
                    {
                        Id = dto.Id,
                        Username = dto.Username ?? string.Empty,
                        RealName = dto.RealName ?? string.Empty,
                        Email = dto.Email,
                        PhoneNumber = dto.PhoneNumber,
                        Role = dto.Role,
                        Department = dto.Department,
                        IsActive = dto.IsActive,
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
                System.Diagnostics.Debug.WriteLine($"加载用户列表异常: {ex.Message}");
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
                // 简单实现：直接创建一个新用户
                MessageBox.Show("用户添加功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加用户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void ExecuteEdit(UserInfo item)
        {
            if (item == null) return;

            try
            {
                // 简单实现：显示用户信息
                MessageBox.Show(
                    $"用户名: {item.Username}\n" +
                    $"真实姓名: {item.RealName}\n" +
                    $"角色: {item.RoleDisplayName}\n" +
                    $"部门: {item.Department ?? "未设置"}\n" +
                    $"状态: {(item.IsActive ? "启用" : "禁用")}",
                    "用户信息", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑用户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}