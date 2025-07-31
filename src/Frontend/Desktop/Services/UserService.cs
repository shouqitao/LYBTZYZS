using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 用户服务实现
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IApiService _apiService;

        public UserService(IApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<PaginatedResult<UserInfo>> SearchUsersAsync(UserQueryRequest request)
        {
            try
            {
                var queryString = BuildQueryString(request);
                var response = await _apiService.GetAsync<SearchUsersResponse>($"api/v1/Users/search?{queryString}");
                
                if (response.IsSuccess && response.Data != null)
                {
                    return new PaginatedResult<UserInfo>
                    {
                        Items = response.Data.users ?? new List<UserInfo>(),
                        TotalCount = response.Data.total,
                        CurrentPage = request.Page,
                        PageSize = request.PageSize
                    };
                }

                return new PaginatedResult<UserInfo>
                {
                    Items = new List<UserInfo>(),
                    TotalCount = 0,
                    CurrentPage = request.Page,
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
        public async Task<ApiResponse<object>> CreateUserAsync(UserCreateRequest request)
        {
            try
            {
                var createDto = new
                {
                    userName = request.UserName,
                    realName = request.RealName,
                    role = request.Role.ToString(),
                    email = request.Email,
                    phoneNumber = request.PhoneNumber
                };

                return await _apiService.PostAsync<object>("api/v1/Users/add", createDto);
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
        public async Task<ApiResponse<object>> UpdateUserAsync(UserUpdateRequest request)
        {
            try
            {
                var updateDto = new
                {
                    id = request.Id,
                    userName = request.UserName,
                    realName = request.RealName,
                    role = request.Role.ToString(),
                    email = request.Email,
                    phoneNumber = request.PhoneNumber,
                    isActive = request.IsActive
                };

                return await _apiService.PutAsync<object>("api/v1/Users/update", updateDto);
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
                return await _apiService.PostAsync<object>($"api/v1/Users/disable/{userId}", new { });
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
                return await _apiService.PostAsync<object>($"api/v1/Users/enable/{userId}", new { });
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
                return await _apiService.PostAsync<object>($"api/v1/Users/resetPassword/{userId}", new { });
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
        public async Task<List<RoleInfo>> GetRolesAsync()
        {
            try
            {
                var response = await _apiService.GetAsync<List<RoleInfo>>("api/v1/Users/getRoles");
                return response.IsSuccess && response.Data != null ? response.Data : new List<RoleInfo>();
            }
            catch (Exception)
            {
                return new List<RoleInfo>();
            }
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ApiResponse<UserInfo>> GetUserByIdAsync(Guid userId)
        {
            try
            {
                return await _apiService.GetAsync<UserInfo>($"api/v1/Users/getById/{userId}");
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
        private string BuildQueryString(UserQueryRequest request)
        {
            var parameters = new List<string>();

            if (!string.IsNullOrEmpty(request.Keyword))
                parameters.Add($"keyword={Uri.EscapeDataString(request.Keyword)}");

            if (request.Role.HasValue)
                parameters.Add($"role={request.Role}");

            if (request.IsActive.HasValue)
                parameters.Add($"isActive={request.IsActive.Value}");

            parameters.Add($"page={request.Page}");
            parameters.Add($"pageSize={request.PageSize}");

            return string.Join("&", parameters);
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