using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading;
using System.Linq.Expressions;

namespace LYBT.Module.Formulas.Interfaces
{
    /// <summary>
    /// 验方仓储接口（Legacy） - 继承BaseRepository提供通用CRUD，扩展验方特定业务方法
    /// </summary>
    public interface IFormulaRepositoryLegacy : IRepository<Formula>
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
        /// 根据传入的谓词条件，查询并返回包含药材配伍的验方列表
        /// </summary>
        Task<List<Formula>> FindWithHerbsAsync(Expression<Func<Formula, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取分页列表（包含药材配伍信息）
        /// </summary>
        Task<PagedResult<Formula>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 获取分页列表（包含药材配伍信息 + category/role 筛选，DB 层执行）
        /// </summary>
        Task<PagedResult<Formula>> GetPagedWithDetailsAsync(
            int pageNumber, int pageSize, string? keyword,
            string? category, Guid? userId, bool isAdmin);

        /// <summary>
        /// 根据用户ID获取方剂列表（包含权限逻辑：自己的+共享的）
        /// </summary>
        Task<List<Formula>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 获取所有验方（包含药材组成），用于导出
        /// </summary>
        Task<List<Formula>> GetAllWithHerbsAsync();

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// </summary>
        Task<Formula?> GetByIdIncludingDeletedAsync(Guid id);
    }
}


