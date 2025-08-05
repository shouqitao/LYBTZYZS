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
                // 使用Refit调用分页API
                var response = await _userApiService.GetPagedUsersAsync(request);
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var users = response.Content.Items.Select(ConvertToUserInfo).ToList();
                    
                    return new LYBT.WPF.Client.Core.Models.Common.PagedResult<UserInfo>
                    {
                        Items = users,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
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
                await _userApiService.UpdateUserAsync(request)
            );
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult> DisableUserAsync(Guid userId)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _userApiService.DisableUserAsync(userId)
            );
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult> EnableUserAsync(Guid userId)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _userApiService.EnableUserAsync(userId)
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
        public Task<List<UserRole>> GetRolesAsync()
        {
            try
            {
                // 直接返回枚举值列表
                return Task.FromResult(Enum.GetValues<UserRole>().ToList());
            }
            catch (Exception)
            {
                return Task.FromResult(new List<UserRole>());
            }
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
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _userApiService.GetActiveUsersAsync()
            );
            
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var users = apiResponse.Data.Select(ConvertToUserInfo).ToList();
                return ServiceResult<List<UserInfo>>.Success(users);
            }
            
            return ServiceResult<List<UserInfo>>.Failure(apiResponse.ErrorMessage ?? "获取活跃用户失败", apiResponse.Exception);
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
        public async Task<ServiceResult> ChangeProfileAsync(string realName, string? email, string? phoneNumber)
        {
            var dto = new ChangeProfileDto { RealName = realName, Email = email, PhoneNumber = phoneNumber };
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

            if (request.Role.HasValue)
                parameters.Add($"role={request.Role}");

            if (request.IsActive.HasValue)
                parameters.Add($"isActive={request.IsActive.Value}");

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
                Role = dto.Role,
                IsActive = dto.IsActive,
                CreateTime = dto.CreateTime,
                LastLoginTime = dto.LastLoginTime,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                IsSuperAdmin = dto.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true
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