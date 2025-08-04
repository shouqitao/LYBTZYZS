using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
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
                
                if (response.IsSuccess && response.Data != null)
                {
                    var users = response.Data.Items.Select(ConvertToUserInfo).ToList();
                    
                    return new LYBT.WPF.Client.Core.Models.Common.PagedResult<UserInfo>
                    {
                        Items = users,
                        TotalCount = response.Data.TotalCount,
                        CurrentPage = response.Data.CurrentPage,
                        PageSize = response.Data.PageSize
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
        public async Task<ApiResponse<object>> CreateUserAsync(UserCreateDto request)
        {
            try
            {
                // 直接使用request，因为现在已经是UserCreateDto类型
                var response = await _userApiService.CreateUserAsync(request);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"创建用户失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task<ApiResponse<object>> UpdateUserAsync(UserUpdateDto request)
        {
            try
            {
                // 直接传递完整的UpdateDto对象
                var response = await _userApiService.UpdateUserAsync(request);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新用户失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ApiResponse<object>> DisableUserAsync(Guid userId)
        {
            try
            {
                var response = await _userApiService.DisableUserAsync(userId);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"禁用用户失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ApiResponse<object>> EnableUserAsync(Guid userId)
        {
            try
            {
                var response = await _userApiService.EnableUserAsync(userId);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"启用用户失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task<ApiResponse<object>> ResetPasswordAsync(Guid userId)
        {
            try
            {
                var response = await _userApiService.ResetPasswordAsync(userId);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"重置密码失败: {ex.Message}"
                };
            }
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
        public async Task<ApiResponse<UserInfo>> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var response = await _userApiService.GetUserByIdAsync(userId);
                if (response.IsSuccess && response.Data != null)
                {
                    return new ApiResponse<UserInfo>
                    {
                        IsSuccess = true,
                        Data = ConvertToUserInfo(response.Data),
                        Message = response.Message
                    };
                }
                return new ApiResponse<UserInfo>
                {
                    IsSuccess = false,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserInfo>
                {
                    IsSuccess = false,
                    Message = $"获取用户详情失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        public async Task<ApiResponse<List<UserInfo>>> GetActiveUsersAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<UserInfo>>("api/v1/Users/active");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<UserInfo>>
                {
                    IsSuccess = false,
                    Message = $"获取活跃用户失败: {ex.Message}",
                    Data = new List<UserInfo>()
                };
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ApiResponse<object>> BatchDisableUsersAsync(List<Guid> userIds)
        {
            try
            {
                var dto = new { ids = userIds };
                return await _apiService.PostAsync<object>("api/v1/Users/batchDisable", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"批量禁用用户失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ApiResponse<object>> BatchEnableUsersAsync(List<Guid> userIds)
        {
            try
            {
                var dto = new { ids = userIds };
                return await _apiService.PostAsync<object>("api/v1/Users/batchEnable", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"批量启用用户失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        public async Task<ApiResponse<object>> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                var dto = new { oldPassword, newPassword };
                return await _apiService.PostAsync<object>("api/v1/Users/changePassword", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"修改密码失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 修改个人信息
        /// </summary>
        public async Task<ApiResponse<object>> ChangeProfileAsync(string realName, string? email, string? phoneNumber)
        {
            try
            {
                var dto = new { realName, email, phoneNumber };
                return await _apiService.PostAsync<object>("api/v1/Users/changeProfile", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"修改个人信息失败: {ex.Message}"
                };
            }
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