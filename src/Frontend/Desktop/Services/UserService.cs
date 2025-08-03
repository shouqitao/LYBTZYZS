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
        public async Task<PaginatedResult<UserInfo>> SearchUsersAsync(UserPagedQueryDto request)
        {
            try
            {
                // 使用Refit调用API
                var response = await _userApiService.GetUsersAsync(request.SearchKeyword);
                
                if (response.IsSuccess && response.Data != null)
                {
                    var users = response.Data.Select(ConvertToUserInfo).ToList();
                    
                    // 应用客户端过滤
                    if (request.Role.HasValue)
                    {
                        users = users.Where(u => u.Role == request.Role.Value).ToList();
                    }
                    if (request.IsActive.HasValue)
                    {
                        users = users.Where(u => u.IsActive == request.IsActive.Value).ToList();
                    }
                    
                    // 分页
                    var totalCount = users.Count;
                    var skip = (request.CurrentPage - 1) * request.PageSize;
                    users = users.Skip(skip).Take(request.PageSize).ToList();
                    
                    return new PaginatedResult<UserInfo>
                    {
                        Items = users,
                        TotalCount = totalCount,
                        CurrentPage = request.CurrentPage,
                        PageSize = request.PageSize
                    };
                }

                return new PaginatedResult<UserInfo>
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
                // UserUpdateDto现在包含Id属性
                var response = await _userApiService.UpdateUserAsync(request.Id, request);
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
                var response = await _userApiService.ToggleUserStatusAsync(userId);
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
                var response = await _userApiService.ToggleUserStatusAsync(userId);
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
        public async Task<List<UserRole>> GetRolesAsync()
        {
            try
            {
                // 直接返回枚举值列表
                return Enum.GetValues<UserRole>().ToList();
            }
            catch (Exception)
            {
                return new List<UserRole>();
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