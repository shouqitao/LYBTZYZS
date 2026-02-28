using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Interfaces
{

    /// <summary>
    /// 验方仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展验方特定业务方法
    /// </summary>
    /// <summary>
    /// 验方仓储接口 - 简化版，减少冗余权限方法
    /// 继承BaseRepository提供通用CRUD，扩展验方特定业务方法
    /// </summary>
    public interface IFormulaRepository : IRepository<Formula>
    {
        /// <summary>
        /// 获取模板验方列表
        /// </summary>
        Task<List<Formula>> GetTemplatesAsync();

        /// <summary>
        /// 根据ID获取方剂（包含所有药材配伍）
        /// </summary>
        Task<Formula> GetByIdWithHerbsAsync(Guid id);

        /// <summary>
        /// 获取分页列表（包含药材配伍信息）
        /// </summary>
        Task<PagedResult<Formula>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 获取分页列表（包含药材配伍信息 + category/role 筛选，DB 层执行）
        /// Sprint3-X6: 从 Service 内存过滤迁移到 Repository DB 查询
        /// </summary>
        Task<PagedResult<Formula>> GetPagedWithDetailsAsync(
            int pageNumber, int pageSize, string? keyword,
            string? category, Guid? userId, bool isAdmin);

        /// <summary>
        /// 根据用户ID获取方剂列表（包含权限逻辑：自己的+共享的）
        /// </summary>
        Task<List<Formula>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// T5-P2-36: 获取所有验方（包含药材组成），用于导出
        /// </summary>
        Task<List<Formula>> GetAllWithHerbsAsync();

        // ========== OpenSpec: optimize-module-list-ui - 恢复功能支持 ==========

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// 用于Restore操作时获取已删除的实体
        /// </summary>
        /// <param name="id">实体ID</param>
        Task<Formula?> GetByIdIncludingDeletedAsync(Guid id);
    }
}
