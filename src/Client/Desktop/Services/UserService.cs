using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Interfaces.Services.Business;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Common;

using LYBT.Desktop.Core.Models.Users;

// UltraThink重构: 统一UserInfo和UserDto，使用UserDto作为统一模型
using UserInfo = LYBT.Shared.Models.Contracts.Users.UserDto;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 用户服务实现
    /// </summary>
    public class UserService : 
        LYBT.Shared.Interfaces.Services.Business.IUserService,
        LYBT.Desktop.Core.Interfaces.Services.IUserService
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
        public async Task<PagedResult<UserInfo>> SearchUsersAsync(UserPagedQueryDto request)
        {
            try
            {
                // 使用更新后的RESTful GET接口
                var response = await _userApiService.GetUsersAsync(
                    page: request.PageIndex,
                    pageSize: request.PageSize,
                    keyword: request.Keyword,
                    username: request.Username,
                    realName: request.RealName,
                    email: request.Email,
                    phoneNumber: request.PhoneNumber,
                    isActive: request.Status == CommonStatus.Enabled ? true : (request.Status == CommonStatus.Disabled ? false : null)
                );

                if (response.IsSuccessStatusCode && response.Content?.Data != null)
                {
                    var users = response.Content.Data.Items.Select(ConvertToUserInfo).ToList();

                    return new PagedResult<UserInfo>
                    {
                        Items = users,
                        TotalCount = (int)response.Content.Data.TotalCount,
                        CurrentPage = response.Content.Data.CurrentPage,
                        PageSize = response.Content.Data.PageSize
                    };
                }

                return new PagedResult<UserInfo>
                {
                    Items = new List<UserInfo>(),
                    TotalCount = 0,
                    CurrentPage = request.PageIndex,
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

            return ServiceResult<UserInfo>.Failure(apiResponse.ErrorMessage ?? "获取用户详情失败", null, apiResponse.Exception);
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
                return ServiceResult<List<UserInfo>>.Failure($"获取活跃用户失败: {ex.Message}", null, ex);
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

            if (!string.IsNullOrEmpty(request.Keyword))
                parameters.Add($"keyword={Uri.EscapeDataString(request.Keyword)}");

            // Role和IsActive已经被移除，不再需要这些参数

            parameters.Add($"page={request.PageIndex}");
            parameters.Add($"pageSize={request.PageSize}");

            return string.Join("&", parameters);
        }

        /// <summary>
        /// UltraThink重构: 已统一UserDto和UserInfo，无需转换
        /// </summary>
        private UserInfo ConvertToUserInfo(LYBT.Shared.Models.Contracts.Users.UserDto dto)
        {
            // 由于type alias，UserInfo和UserDto现在是同一类型，直接返回
            return dto;
        }

        #region 新业务接口实现

        /// <summary>
        /// 分页查询用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<UserDto>>> LYBT.Shared.Interfaces.Services.Business.IUserService.GetPagedAsync(UserPagedQueryDto query)
        {
            try
            {
                var uiResult = await SearchUsersAsync(query);
                var businessResult = new LYBT.Shared.Models.Contracts.Common.PagedResult<UserDto>
                {
                    Items = uiResult.Items,
                    TotalCount = uiResult.TotalCount,
                    CurrentPage = uiResult.CurrentPage,
                    PageSize = uiResult.PageSize
                };
                return ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<UserDto>>.Success(businessResult);
            }
            catch (Exception ex)
            {
                return ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<UserDto>>.Failure($"分页查询用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据ID获取用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<UserDto>> LYBT.Shared.Interfaces.Services.Business.IUserService.GetByIdAsync(Guid id)
        {
            var result = await GetUserByIdAsync(id);
            return result; // UserInfo和UserDto是同一类型
        }

        /// <summary>
        /// 根据用户名获取用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<UserDto>> LYBT.Shared.Interfaces.Services.Business.IUserService.GetByUsernameAsync(string username)
        {
            try
            {
                // 通过搜索实现
                var query = new UserPagedQueryDto { Username = username, PageSize = 1 };
                var result = await SearchUsersAsync(query);
                var user = result.Items.FirstOrDefault();
                if (user != null)
                {
                    return ServiceResult<UserDto>.Success(user);
                }
                return ServiceResult<UserDto>.Failure("用户不存在");
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"获取用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<UserDto>> LYBT.Shared.Interfaces.Services.Business.IUserService.CreateAsync(UserCreateDto dto)
        {
            var createResult = await CreateUserAsync(dto);
            if (createResult.IsSuccess)
            {
                // 创建成功后获取用户详情
                try
                {
                    await Task.Delay(100); // 等待数据库同步
                    var userResult = await ((LYBT.Shared.Interfaces.Services.Business.IUserService)this).GetByUsernameAsync(dto.Username);
                    return userResult;
                }
                catch
                {
                    return ServiceResult<UserDto>.Success(new UserDto { Username = dto.Username, RealName = dto.RealName });
                }
            }
            return ServiceResult<UserDto>.Failure(createResult.ErrorMessage ?? "创建用户失败");
        }

        /// <summary>
        /// 更新用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<UserDto>> LYBT.Shared.Interfaces.Services.Business.IUserService.UpdateAsync(Guid id, UserUpdateDto dto)
        {
            var updateResult = await UpdateUserAsync(dto);
            if (updateResult.IsSuccess)
            {
                var userResult = await GetUserByIdAsync(id);
                return userResult;
            }
            return ServiceResult<UserDto>.Failure(updateResult.ErrorMessage ?? "更新用户失败");
        }

        /// <summary>
        /// 启用用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.EnableAsync(Guid id)
        {
            return await EnableUserAsync(id);
        }

        /// <summary>
        /// 禁用用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.DisableAsync(Guid id)
        {
            return await DisableUserAsync(id);
        }

        /// <summary>
        /// 重置密码 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.ResetPasswordAsync(Guid id, string? newPassword = null)
        {
            return await ResetPasswordAsync(id);
        }

        /// <summary>
        /// 修改密码 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            return await ChangePasswordAsync(oldPassword, newPassword);
        }

        /// <summary>
        /// 搜索用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<List<UserDto>>> LYBT.Shared.Interfaces.Services.Business.IUserService.SearchAsync(string keyword)
        {
            try
            {
                var query = new UserPagedQueryDto { Keyword = keyword, PageSize = 100 };
                var result = await SearchUsersAsync(query);
                return ServiceResult<List<UserDto>>.Success(result.Items);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserDto>>.Failure($"搜索用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 按角色获取用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<List<UserDto>>> LYBT.Shared.Interfaces.Services.Business.IUserService.GetByRoleAsync(string role)
        {
            try
            {
                // 简化实现：系统目前没有复杂角色管理
                var activeUsers = await GetActiveUsersAsync();
                return ServiceResult<List<UserDto>>.Success(activeUsers.Data ?? new List<UserDto>());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserDto>>.Failure($"获取角色用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取活跃用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<List<UserDto>>> LYBT.Shared.Interfaces.Services.Business.IUserService.GetActiveAsync()
        {
            return await GetActiveUsersAsync();
        }

        /// <summary>
        /// 获取在线用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<List<UserDto>>> LYBT.Shared.Interfaces.Services.Business.IUserService.GetOnlineAsync()
        {
            // 简化实现：当前系统未跟踪在线状态，返回活跃用户
            return await GetActiveUsersAsync();
        }

        /// <summary>
        /// 批量启用用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.BatchEnableAsync(List<Guid> ids)
        {
            return await BatchEnableUsersAsync(ids);
        }

        /// <summary>
        /// 批量禁用用户 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.BatchDisableAsync(List<Guid> ids)
        {
            return await BatchDisableUsersAsync(ids);
        }

        /// <summary>
        /// 检查用户名是否存在 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.Business.IUserService.IsUsernameExistsAsync(string username, Guid? excludeId = null)
        {
            try
            {
                var userResult = await ((LYBT.Shared.Interfaces.Services.Business.IUserService)this).GetByUsernameAsync(username);
                if (userResult.IsSuccess && userResult.Data != null)
                {
                    if (excludeId.HasValue && userResult.Data.Id == excludeId.Value)
                    {
                        return ServiceResult<bool>.Success(false);
                    }
                    return ServiceResult<bool>.Success(true);
                }
                return ServiceResult<bool>.Success(false);
            }
            catch
            {
                return ServiceResult<bool>.Success(false);
            }
        }

        /// <summary>
        /// 获取统计信息 - 业务接口实现
        /// </summary>
        async Task<ServiceResult<UserStatisticsDto>> LYBT.Shared.Interfaces.Services.Business.IUserService.GetStatisticsAsync()
        {
            try
            {
                var activeUsers = await GetActiveUsersAsync();
                var stats = new UserStatisticsDto
                {
                    TotalCount = activeUsers.Data?.Count ?? 0,
                    ActiveCount = activeUsers.Data?.Count ?? 0,
                    InactiveCount = 0 // 当前系统未跟踪非活跃状态
                };
                return ServiceResult<UserStatisticsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserStatisticsDto>.Failure($"获取统计信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新最后登录信息 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.UpdateLastLoginAsync(Guid id, DateTime loginTime, string loginIp)
        {
            // 简化实现：当前系统未跟踪登录信息
            return ServiceResult.Success();
        }

        /// <summary>
        /// 设置在线状态 - 业务接口实现
        /// </summary>
        async Task<ServiceResult> LYBT.Shared.Interfaces.Services.Business.IUserService.SetOnlineStatusAsync(Guid id, bool isOnline)
        {
            // 简化实现：当前系统未跟踪在线状态
            return ServiceResult.Success();
        }

        #endregion

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