using LYBT.Entities.Users;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户仓储接口 - 简化版本，只保留最基础的方法
    /// </summary>
    /// <summary>
    /// 用户仓储接口 - 继承IRepository<User>标准接口
    /// Phase 1 Task 1.2: 实现基础数据模块统一Repository规范
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - ⭐ 统一共性：继承IRepository<User>获得11个标准CRUD方法
    /// - ⭐ 保持特性：保留用户模块特定业务方法
    /// 
    /// 特定业务方法说明：
    /// - GetByUsernameAsync: 用户名登录查询
    /// - UsernameExistsAsync: 用户名唯一性校验
    /// </remarks>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// 根据用户名获取用户（支持用户名或邮箱登录）
        /// </summary>
        /// <param name="username">用户名或邮箱</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户对象，不存在时返回null</returns>
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户名是否已存在
        /// </summary>
        /// <param name="username">待检查的用户名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页查询用户（支持 keyword/role/status 筛选，DB 层执行）
        /// Sprint3-X6: 从 Service 内存过滤迁移到 Repository DB 查询
        /// </summary>
        Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize, string? keyword, UserRole? role, CommonStatus? status, CancellationToken cancellationToken = default);

        // ========== OpenSpec: optimize-module-list-ui - 恢复功能支持 ==========

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// 用于Restore操作时获取已删除的实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
