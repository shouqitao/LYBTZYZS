using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 用户服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 分页查询用户
        /// </summary>
        Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新用户
        /// </summary>
        Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto);

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}