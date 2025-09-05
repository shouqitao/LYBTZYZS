using LYBT.Entities.Users;
using LYBT.Infrastructure.Interfaces;
using SharedUserPagedQueryDto = LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto;

namespace LYBT.Module.Users.Interfaces
{

    /// <summary>
    /// 用户仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展用户特定业务方法
    /// </summary>
    public interface IUserRepository : IBaseRepository<User>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义用户特有的业务方法

        /// <summary>
        /// 禁用用户（将用户状态设为禁用）
        /// </summary>
        Task<bool> DisableAsync(Guid id);

        /// <summary>
        /// 启用用户（将用户状态设为启用）
        /// </summary>
        Task<bool> EnableAsync(Guid id);

        /// <summary>
        /// 分页条件查找用户（支持关键词、角色、状态筛选）
        /// 管理员可以查询所有用户（包括禁用的），普通用户只能查询启用的用户
        /// </summary>
        Task<(IList<User> users, int total)> GetPagedAsync(SharedUserPagedQueryDto query, bool includeDisabled = false);

        /// <summary>
        /// 根据用户名查找用户（登录或唯一性校验）
        /// </summary>
        Task<User?> GetByUsernameAsync(string userName);

        /// <summary>
        /// 根据用户ID查找（支持权限控制）
        /// 管理员可以查询所有用户，普通用户只能查询启用的用户
        /// </summary>
        Task<User?> GetByIdAsync(Guid id, bool includeDisabled = false);

        /// <summary>
        /// 根据ID列表批量获取用户
        /// </summary>
        Task<List<User>> GetUsersByIdsAsync(List<Guid> ids, bool includeDisabled = false);

        /// <summary>
        /// 校验用户名是否存在（包括禁用用户）
        /// </summary>
        Task<bool> ExistsByUsernameAsync(string userName);

        /// <summary>
        /// 更新用户密码
        /// </summary>
        Task<bool> UpdatePasswordAsync(Guid id, string passwordHash);

        /// <summary>
        /// 批量更新启用状态
        /// </summary>
        Task<int> UpdateActiveStatusAsync(List<Guid> ids, bool isActive);

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        Task<List<User>> GetActiveUsersAsync();
    }
}
