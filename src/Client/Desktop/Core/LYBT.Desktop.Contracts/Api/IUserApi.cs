using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 用户API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IUserApi
    {
        /// <summary>
        /// 获取用户列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/users")]
        Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Refit.Get("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Refit.Post("/api/v1/users")]
        Task<ApiResponse<UserDto>> CreateUserAsync([Refit.Body] UserInputDto request);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Refit.Put("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, [Refit.Body] UserInputDto request);

        /// <summary>
        /// 删除用户
        /// </summary>
        [Refit.Delete("/api/v1/users/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteUserAsync(Guid id);
    }
}
