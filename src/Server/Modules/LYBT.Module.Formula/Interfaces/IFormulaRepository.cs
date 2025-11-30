using LYBT.Shared.Models.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formula.Interfaces
{

    /// <summary>
    /// 验方仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展验方特定业务方法
    /// </summary>
    /// <summary>
    /// 验方仓储接口 - 简化版，减少冗余权限方法
    /// 继承BaseRepository提供通用CRUD，扩展验方特定业务方法
    /// </summary>
    public interface IFormulaRepository : IRepository<FormulaEntity>
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
        /// 获取分页列表（包含药材配伍信息）
        /// </summary>
        Task<PagedResult<FormulaEntity>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据用户ID获取方剂列表（包含权限逻辑：自己的+共享的）
        /// </summary>
        Task<List<FormulaEntity>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 获取共享的方剂列表（保留用于特殊场景）
        /// </summary>
        Task<List<FormulaEntity>> GetSharedFormulasAsync();

        /// <summary>
        /// 根据类别获取方剂列表
        /// </summary>
        Task<List<FormulaEntity>> GetByCategoryAsync(string category);
    }
}
