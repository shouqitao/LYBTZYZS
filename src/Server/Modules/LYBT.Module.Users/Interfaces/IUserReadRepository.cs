using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户只读仓储接口 - 专门为QueryService提供数据访问
    /// 继承IReadOnlyRepository提供基础查询功能，扩展用户特定的查询方法
    /// </summary>
    public interface IUserReadRepository : IReadOnlyRepository<LYBT.Entities.Users.User>
    {
        /// <summary>
        /// 根据ID获取用户详情DTO
        /// </summary>
        Task<UserDto?> GetUserDtoByIdAsync(Guid id);

        /// <summary>
        /// 分页查询用户并映射为DTO
        /// </summary>
        Task<PagedResult<UserDto>> GetPagedUserDtosAsync(UserSearchDto query);

        /// <summary>
        /// 根据用户名获取用户DTO
        /// </summary>
        Task<UserDto?> GetUserDtoByUsernameAsync(string userName);

        /// <summary>
        /// 获取启用的用户DTO列表
        /// </summary>
        Task<List<UserDto>> GetActiveUserDtosAsync();

        /// <summary>
        /// 搜索用户并映射为DTO
        /// </summary>
        Task<List<UserDto>> SearchUserDtosAsync(string keyword, int maxResults = 50);

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        Task<bool> IsUsernameAvailableAsync(string userName);

        /// <summary>
        /// 获取所有医生DTO列表
        /// </summary>
        Task<List<UserDto>> GetDoctorDtosAsync();

        /// <summary>
        /// 检查医生可用性
        /// </summary>
        Task<bool> IsDoctorAvailableAsync(Guid doctorId);

        /// <summary>
        /// 获取系统角色枚举信息
        /// </summary>
        Task<List<object>> GetRolesAsync();
    }
}