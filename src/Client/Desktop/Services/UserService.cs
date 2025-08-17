using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Desktop.Core.Extensions;

// UltraThink重构: 恢复四层架构清晰分离，UserInfo为UI层，UserDto为传输层

namespace LYBT.Desktop.Services {

    /// <summary>
    /// 用户服务实现
    /// </summary>
    public class UserService : LYBT.Shared.Interfaces.Services.IUserService {
        private readonly IApiService _apiService;
        private readonly IUserApiService _userApiService;

        public UserService(IApiService apiService, IUserApiService userApiService) {
            _apiService = apiService;
            _userApiService = userApiService;
        }

        /// <summary>
        /// 分页查询用户
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        {
            try
            {
                // 使用更新后的RESTful GET接口
                var response = await _userApiService.GetUsersAsync(
                    page: query.PageIndex,
                    pageSize: query.PageSize,
                    keyword: query.Keyword,
                    username: query.Username,
                    realName: query.RealName,
                    email: query.Email,
                    phoneNumber: query.PhoneNumber,
                    isActive: query.Status == CommonStatus.Enabled ? true : (query.Status == CommonStatus.Disabled ? false : null)
                );

                if (response.IsSuccessStatusCode && response.Content?.Data != null)
                {
                    var result = new PagedResult<UserDto>
                    {
                        Items = response.Content.Data.Items.ToList(),
                        TotalCount = (int)response.Content.Data.TotalCount,
                        CurrentPage = response.Content.Data.CurrentPage,
                        PageSize = response.Content.Data.PageSize
                    };

                    return ServiceResult<PagedResult<UserDto>>.Success(result);
                }

                var emptyResult = new PagedResult<UserDto>
                {
                    Items = new List<UserDto>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<UserDto>>.Success(emptyResult);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserDto>>.Failure($"分页查询用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 搜索用户（保留UI层兼容方法）
        /// </summary>
        public async Task<PagedResult<UserInfo>> SearchUsersAsync(UserPagedQueryDto request)
        {
            try
            {
                var result = await GetPagedAsync(request);
                if (result.IsSuccess && result.Data != null)
                {
                    var users = result.Data.Items.ToUserInfoList();

                    return new PagedResult<UserInfo>
                    {
                        Items = users,
                        TotalCount = result.Data.TotalCount,
                        CurrentPage = result.Data.CurrentPage,
                        PageSize = result.Data.PageSize
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
        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        {
            try
            {
                var response = await _userApiService.CreateUserAsync(dto);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true && response.Content.Data != null)
                {
                    return ServiceResult<UserDto>.Success(response.Content.Data);
                }

                var errorMessage = response.Content?.Message ?? "创建用户失败";
                return ServiceResult<UserDto>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"创建用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 新增用户（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> CreateUserAsync(UserCreateDto request)
        {
            var result = await CreateAsync(request);
            return result.IsSuccess 
                ? ServiceResult.Success(result.Data)
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            try
            {
                var response = await _userApiService.UpdateUserAsync(id, dto);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true && response.Content.Data != null)
                {
                    return ServiceResult<UserDto>.Success(response.Content.Data);
                }

                var errorMessage = response.Content?.Message ?? "更新用户失败";
                return ServiceResult<UserDto>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"更新用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新用户（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> UpdateUserAsync(UserUpdateDto request)
        {
            var result = await UpdateAsync(request.Id, request);
            return result.IsSuccess 
                ? ServiceResult.Success(result.Data)
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                var response = await _userApiService.ToggleStatusAsync(id);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<bool>.Success(true);
                }

                var errorMessage = response.Content?.Message ?? "禁用用户失败";
                return ServiceResult<bool>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"禁用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                var response = await _userApiService.ToggleStatusAsync(id);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<bool>.Success(true);
                }

                var errorMessage = response.Content?.Message ?? "启用用户失败";
                return ServiceResult<bool>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"启用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 禁用用户（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> DisableUserAsync(Guid userId)
        {
            var result = await DisableAsync(userId);
            return result.IsSuccess 
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 启用用户（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> EnableUserAsync(Guid userId)
        {
            var result = await EnableAsync(userId);
            return result.IsSuccess 
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                var response = await _userApiService.ResetPasswordAsync(id);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<bool>.Success(true);
                }

                var errorMessage = response.Content?.Message ?? "重置用户密码失败";
                return ServiceResult<bool>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"重置用户密码失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重置用户密码（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> ResetPasswordAsync(Guid userId)
        {
            var result = await ResetPasswordAsync(userId, "");
            return result.IsSuccess 
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            try
            {
                var roles = new List<object> { 
                    new { Value = "Admin", DisplayName = "系统管理员" },
                    new { Value = "User", DisplayName = "普通用户" }
                };
                return ServiceResult<List<object>>.Success(roles);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<object>>.Failure($"获取角色列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有角色（保留UI层兼容方法）
        /// </summary>
        public Task<List<string>> GetRolesForUIAsync()
        {
            // 系统只有两种用户类型
            return Task.FromResult(new List<string> { "系统管理员", "普通用户" });
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _userApiService.GetUserByIdAsync(id);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true && response.Content.Data != null)
                {
                    return ServiceResult<UserDto>.Success(response.Content.Data);
                }

                var errorMessage = response.Content?.Message ?? "获取用户详情失败";
                return ServiceResult<UserDto>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"获取用户详情失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据ID获取用户详情（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult<UserInfo>> GetUserByIdAsync(Guid userId)
        {
            var result = await GetByIdAsync(userId);
            if (result.IsSuccess && result.Data != null)
            {
                return ServiceResult<UserInfo>.Success(result.Data.ToUserInfo());
            }

            return ServiceResult<UserInfo>.Failure(result.ErrorMessage ?? "获取用户详情失败", result.Exception);
        }

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            try
            {
                var response = await _userApiService.GetActiveUsersAsync();

                if (response.IsSuccessStatusCode && response.Content?.Success == true && response.Content.Data != null)
                {
                    var users = response.Content.Data.ToList();
                    return ServiceResult<List<UserDto>>.Success(users);
                }

                var errorMessage = response.Content?.Message ?? "获取活跃用户失败";
                return ServiceResult<List<UserDto>>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserDto>>.Failure($"获取活跃用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取活跃用户列表（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult<List<UserInfo>>> GetActiveUsersForUIAsync()
        {
            try
            {
                var result = await GetActiveUsersAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    var users = result.Data.ToUserInfoList();
                    return ServiceResult<List<UserInfo>>.Success(users);
                }

                return ServiceResult<List<UserInfo>>.Failure(result.ErrorMessage ?? "获取活跃用户失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserInfo>>.Failure($"获取活跃用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                var dto = new BatchIdsDto { Ids = ids };
                var response = await _userApiService.BatchDisableAsync(dto);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<int>.Success(ids.Count);
                }

                var errorMessage = response.Content?.Message ?? "批量禁用用户失败";
                return ServiceResult<int>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量禁用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                var dto = new BatchIdsDto { Ids = ids };
                var response = await _userApiService.BatchEnableAsync(dto);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<int>.Success(ids.Count);
                }

                var errorMessage = response.Content?.Message ?? "批量启用用户失败";
                return ServiceResult<int>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量启用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量禁用用户（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> BatchDisableUsersAsync(List<Guid> userIds)
        {
            var result = await BatchDisableAsync(userIds);
            return result.IsSuccess 
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 批量启用用户（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> BatchEnableUsersAsync(List<Guid> userIds)
        {
            var result = await BatchEnableAsync(userIds);
            return result.IsSuccess 
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                var dto = new ChangePasswordDto { OldPassword = oldPassword, NewPassword = newPassword };
                var response = await _userApiService.ChangePasswordAsync(dto);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<bool>.Success(true);
                }

                var errorMessage = response.Content?.Message ?? "修改用户密码失败";
                return ServiceResult<bool>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"修改用户密码失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 修改用户密码（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            // 从当前上下文获取用户ID，这里简化为默认值
            var userId = Guid.Empty;
            var result = await ChangePasswordAsync(userId, oldPassword, newPassword);
            return result.IsSuccess 
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 修改个人信息
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid id, string realName, string phoneNumber)
        {
            try
            {
                var dto = new ChangeProfileDto 
                {
                    RealName = realName,
                    PhoneNumber = phoneNumber
                };
                var response = await _userApiService.ChangeProfileAsync(dto);
                
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<bool>.Success(true);
                }

                var errorMessage = response.Content?.Message ?? "修改个人信息失败";
                return ServiceResult<bool>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"修改个人信息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 修改个人信息（保留UI层兼容方法）
        /// </summary>
        public async Task<ServiceResult> ChangeProfileAsync(string realName, string? phoneNumber)
        {
            // 从当前上下文获取用户ID，这里简化为默认值
            var userId = Guid.Empty;
            var result = await ChangeProfileAsync(userId, realName, phoneNumber ?? "");
            return result.IsSuccess 
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage, result.Exception);
        }

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        private string BuildQueryString(UserPagedQueryDto request) {
            var parameters = new List<string>();

            if (!string.IsNullOrEmpty(request.Keyword))
                parameters.Add($"keyword={Uri.EscapeDataString(request.Keyword)}");

            // Role和IsActive已经被移除，不再需要这些参数

            parameters.Add($"page={request.PageIndex}");
            parameters.Add($"pageSize={request.PageSize}");

            return string.Join("&", parameters);
        }

        // UltraThink重构: 私有转换方法已迁移到DtoConversionExtensions.cs
        // 所有转换现在使用统一的扩展方法

        #region 新业务接口实现

        /// <summary>
        /// 分页查询用户 - 业务接口实现
        /// </summary>

        #endregion 新业务接口实现

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            // 使用禁用代替删除（软删除策略）
            return await DisableAsync(id);
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            try
            {
                // 通过分页查询实现，限制用户名精确匹配
                var query = new UserPagedQueryDto
                {
                    Username = username,
                    PageIndex = 1,
                    PageSize = 1
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data?.Items.Any() == true)
                {
                    var user = result.Data.Items.FirstOrDefault(u => u.Username == username);
                    if (user != null)
                    {
                        return ServiceResult<UserDto>.Success(user);
                    }
                }

                return ServiceResult<UserDto>.Failure("用户不存在");
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"根据用户名获取用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                var query = new UserPagedQueryDto
                {
                    Keyword = keyword,
                    PageIndex = 1,
                    PageSize = 100 // 搜索返回前100个结果
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    return ServiceResult<List<UserDto>>.Success(result.Data.Items.ToList());
                }

                return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage ?? "搜索用户失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserDto>>.Failure($"搜索用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync()
        {
            try
            {
                // 通过获取所有用户计算统计信息
                var activeResult = await GetActiveUsersAsync();
                var allResult = await GetPagedAsync(new UserPagedQueryDto { PageIndex = 1, PageSize = 1000 });

                if (activeResult.IsSuccess && allResult.IsSuccess)
                {
                    var statistics = new
                    {
                        TotalCount = allResult.Data?.TotalCount ?? 0,
                        ActiveCount = activeResult.Data?.Count ?? 0,
                        InactiveCount = (allResult.Data?.TotalCount ?? 0) - (activeResult.Data?.Count ?? 0),
                        RecentCount = 0 // 简化实现
                    };

                    return ServiceResult<object>.Success(statistics);
                }

                return ServiceResult<object>.Failure("获取用户统计信息失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<object>.Failure($"获取用户统计信息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        {
            try
            {
                var result = await GetByUsernameAsync(username);
                return ServiceResult<bool>.Success(!result.IsSuccess); // 获取失败说明用户名可用
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"验证用户名失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
        {
            try
            {
                // 简化实现，返回空日志列表
                var result = new PagedResult<object>
                {
                    Items = new List<object>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<object>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<object>>.Failure($"获取用户操作日志失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有用户（保留UI层兼容方法）
        /// </summary>
        public async Task<List<UserInfo>> GetUsersAsync()
        {
            var result = await GetActiveUsersForUIAsync();
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
    public class SearchUsersResponse {
        public List<UserInfo>? users { get; set; }
        public int total { get; set; }
    }
}