using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Core;

using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 用户服务实现
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IApiService _apiService;
        private readonly IUserApiService _userApiService;

        public UserService(IApiService apiService, IUserApiService userApiService)
        {
            _apiService = apiService;
            _userApiService = userApiService;
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<LYBT.WPF.Client.Core.Models.Common.PagedResult<UserInfo>> SearchUsersAsync(UserPagedQueryDto request)
        {
            try
            {
                // 使用更新后的RESTful GET接口
                var response = await _userApiService.GetUsersAsync(
                    page: request.CurrentPage,
                    pageSize: request.PageSize,
                    keyword: request.SearchKeyword,
                    username: request.Username,
                    realName: request.RealName,
                    email: request.Email,
                    phoneNumber: request.PhoneNumber,
                    isActive: request.Status == CommonStatus.Enabled ? true : (request.Status == CommonStatus.Disabled ? false : null)
                );

                if (response.IsSuccessStatusCode && response.Content?.Data != null)
                {
                    var users = response.Content.Data.Items.Select(ConvertToUserInfo).ToList();

                    return new LYBT.WPF.Client.Core.Models.Common.PagedResult<UserInfo>
                    {
                        Items = users,
                        TotalCount = (int)response.Content.Data.TotalCount,
                        CurrentPage = response.Content.Data.CurrentPage,
                        PageSize = response.Content.Data.PageSize
                    };
                }

                return new LYBT.WPF.Client.Core.Models.Common.PagedResult<UserInfo>
                {
                    Items = new List<UserInfo>(),
                    TotalCount = 0,
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"搜索用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        public async Task<ServiceResult> CreateUserAsync(UserCreateDto request)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.CreateUserAsync(request)
            );
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task<ServiceResult> UpdateUserAsync(UserUpdateDto request)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.UpdateUserAsync(request.Id, request)
            );
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult> DisableUserAsync(Guid userId)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.ToggleStatusAsync(userId)
            );
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult> EnableUserAsync(Guid userId)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.ToggleStatusAsync(userId)
            );
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task<ServiceResult> ResetPasswordAsync(Guid userId)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.ResetPasswordAsync(userId)
            );
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public Task<List<string>> GetRolesAsync()
        {
            // 系统只有两种用户类型
            return Task.FromResult(new List<string> { "系统管理员", "普通用户" });
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserInfo>> GetUserByIdAsync(Guid userId)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _userApiService.GetUserByIdAsync(userId)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<UserInfo>.Success(ConvertToUserInfo(apiResponse.Data));
            }

            return ServiceResult<UserInfo>.Failure(apiResponse.ErrorMessage ?? "获取用户详情失败", apiResponse.Exception);
        }

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserInfo>>> GetActiveUsersAsync()
        {
            try
            {
                var apiResponse = await _userApiService.GetActiveUsersAsync();
                
                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var users = apiResponse.Content.Select(ConvertToUserInfo).ToList();
                    return ServiceResult<List<UserInfo>>.Success(users);
                }

                return ServiceResult<List<UserInfo>>.Failure("获取活跃用户失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserInfo>>.Failure($"获取活跃用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult> BatchDisableUsersAsync(List<Guid> userIds)
        {
            var dto = new BatchIdsDto { Ids = userIds };
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.BatchDisableAsync(dto)
            );
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult> BatchEnableUsersAsync(List<Guid> userIds)
        {
            var dto = new BatchIdsDto { Ids = userIds };
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.BatchEnableAsync(dto)
            );
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        public async Task<ServiceResult> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            var dto = new ChangePasswordDto { OldPassword = oldPassword, NewPassword = newPassword };
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.ChangePasswordAsync(dto)
            );
        }

        /// <summary>
        /// 修改个人信息
        /// </summary>
        public async Task<ServiceResult> ChangeProfileAsync(string realName, string? phoneNumber)
        {
            var dto = new ChangeProfileDto
            {
                RealName = realName,
                PhoneNumber = phoneNumber
            };
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _userApiService.ChangeProfileAsync(dto)
            );
        }

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        private string BuildQueryString(UserPagedQueryDto request)
        {
            var parameters = new List<string>();

            if (!string.IsNullOrEmpty(request.SearchKeyword))
                parameters.Add($"keyword={Uri.EscapeDataString(request.SearchKeyword)}");

            // Role和IsActive已经被移除，不再需要这些参数

            parameters.Add($"page={request.CurrentPage}");
            parameters.Add($"pageSize={request.PageSize}");

            return string.Join("&", parameters);
        }

        /// <summary>
        /// 转换UserDto到UserInfo
        /// </summary>
        private UserInfo ConvertToUserInfo(LYBT.Shared.Models.Contracts.Users.UserDto dto)
        {
            return new UserInfo
            {
                Id = dto.Id,
                Username = dto.Username,
                RealName = dto.RealName,
                Status = dto.Status,
                CreateTime = dto.CreateTime,
                LastLoginTime = dto.LastLoginTime,
                PhoneNumber = dto.PhoneNumber
            };
        }

        /// <summary>
        /// 获取所有用户
        /// </summary>
        public async Task<List<UserInfo>> GetUsersAsync()
        {
            var result = await GetActiveUsersAsync();
            if (result.IsSuccess && result.Data != null)
            {
                return result.Data;
            }
            return new List<UserInfo>();
        }
    }

    /// <summary>
    /// 搜索用户响应
    /// </summary>
    public class SearchUsersResponse
    {
        public List<UserInfo>? users { get; set; }
        public int total { get; set; }
    }
}