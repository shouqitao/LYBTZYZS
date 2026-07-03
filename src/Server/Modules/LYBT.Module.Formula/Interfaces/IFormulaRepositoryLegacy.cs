using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading;
using System.Linq.Expressions;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Interfaces
{
    /// <summary>
    /// 验方仓储接口（Legacy） - 继承BaseRepository提供通用CRUD，扩展验方特定业务方法
    /// </summary>
    public interface IFormulaRepositoryLegacy : IRepository<FormulaEntity>
    {
        /// <summary>
        /// 获取模板验方列表
        /// </summary>
        Task<List<FormulaEntity>> GetTemplatesAsync();

        /// <summary>
        /// 根据ID获取方剂（包含所有药材配伍）
        /// </summary>
        Task<FormulaEntity> GetByIdWithHerbsAsync(Guid id);

        /// <summary>
        /// 根据传入的谓词条件，查询并返回包含药材配伍的验方列表
        /// </summary>
        Task<List<FormulaEntity>> FindWithHerbsAsync(Expression<Func<FormulaEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取分页列表（包含药材配伍信息）
        /// </summary>
        Task<PagedResult<FormulaEntity>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 获取分页列表（包含药材配伍信息 + category/role 筛选，DB 层执行）
        /// </summary>
        Task<PagedResult<FormulaEntity>> GetPagedWithDetailsAsync(
            int pageNumber, int pageSize, string? keyword,
            string? category, Guid? userId, bool isAdmin);

        /// <summary>
        /// 根据用户ID获取方剂列表（包含权限逻辑：自己的+共享的）
        /// </summary>
        Task<List<FormulaEntity>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 获取所有验方（包含药材组成），用于导出
        /// </summary>
        Task<List<FormulaEntity>> GetAllWithHerbsAsync();

        /// <summary>
        /// 按分类筛选验方（包含药材组成，DB层执行过滤），用于导出
        /// </summary>
        Task<List<FormulaEntity>> GetByCategoryWithHerbsAsync(string category);

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// </summary>
        Task<FormulaEntity?> GetByIdIncludingDeletedAsync(Guid id);
    }
}


