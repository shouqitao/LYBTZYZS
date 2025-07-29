using Refit;
using LYBT.UI.PrismWpf.Models;
using LYBT.Common.Responses;
using LYBT.Models.Users;

namespace LYBT.UI.PrismWpf.Services.Api
{
    /// <summary>
    /// 用户管理API接口
    /// </summary>
    public interface IUsersApi
    {
        /// <summary>
        /// 获取用户列表（分页）
        /// </summary>
        [Get("/api/Users")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<PagedResult<UserModel>>> GetUsersAsync(
            [Query] int page = 1, 
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] bool includeDisabled = false);

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        [Get("/api/Users/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<UserModel>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Post("/api/Users")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<UserModel>> CreateUserAsync([Body] CreateUserRequest request);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Put("/api/Users/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<UserModel>> UpdateUserAsync(Guid id, [Body] UpdateUserRequest request);

        /// <summary>
        /// 启用/禁用用户
        /// </summary>
        [Put("/api/Users/{id}/status")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<object>> UpdateUserStatusAsync(Guid id, [Body] UpdateStatusRequest request);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [Put("/api/Users/{id}/reset-password")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<object>> ResetPasswordAsync(Guid id);

        /// <summary>
        /// 批量启用/禁用用户
        /// </summary>
        [Put("/api/Users/batch-status")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<object>> BatchUpdateStatusAsync([Body] BatchUpdateStatusRequest request);
    }

    /// <summary>
    /// 创建用户请求
    /// </summary>
    public class CreateUserRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string PinyinCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Password { get; set; }
    }

    /// <summary>
    /// 更新用户请求
    /// </summary>
    public class UpdateUserRequest
    {
        public string RealName { get; set; } = string.Empty;
        public string PinyinCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// 更新状态请求
    /// </summary>
    public class UpdateStatusRequest
    {
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 批量更新状态请求
    /// </summary>
    public class BatchUpdateStatusRequest
    {
        public List<Guid> UserIds { get; set; } = new();
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }
}