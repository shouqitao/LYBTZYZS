using LYBT.Entities.Users;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户仓储接口 - 简化版本，合并读写操作
    /// 继承 IRepository 提供通用CRUD，扩展用户特定方法
    /// 仓储只返回实体，由服务层负责 DTO 映射
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        // 基础查询方法
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<List<User>> GetByRoleAsync(UserRole role);
        Task<PagedResult<User>> GetPagedAsync(PagedQueryBaseDto query);
        Task<List<User>> SearchAsync(string? keyword = null, UserRole? role = null, CommonStatus? status = null, int maxResults = 50);

        // 存在性检查方法
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);

        // 状态管理方法
        Task<bool> EnableAsync(Guid id);
        Task<bool> DisableAsync(Guid id);
        Task<int> GetOnlineCountAsync();

        // 批量操作方法
        Task<List<User>> GetUsersByIdsAsync(List<Guid> ids);
        Task<int> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status);
    }
}
