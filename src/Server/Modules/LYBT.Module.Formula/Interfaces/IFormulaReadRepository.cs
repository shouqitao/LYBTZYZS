using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Interfaces
{
    /// <summary>
    /// 验方只读仓储接口 - 提供验方查询相关功能
    /// 包含分页查询、搜索、分类统计等只读操作
    /// </summary>
    public interface IFormulaReadRepository : IReadOnlyRepository<LYBT.Entities.Formula.Formula>
    {
        /// <summary>
        /// 分页查询验方DTO
        /// </summary>
        /// <param name="query">查询条件，包含分页参数和筛选条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验方分页结果</returns>
        Task<PagedResult<FormulaDto>> GetPagedFormulaDtosAsync(FormulaQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索验方DTO（分页）
        /// </summary>
        /// <param name="query">搜索查询条件，支持关键字搜索验方名称、功效、用法</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>搜索结果分页数据</returns>
        Task<PagedResult<FormulaDto>> SearchFormulaDtosAsync(PagedQueryBaseDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取验方DTO列表
        /// </summary>
        /// <param name="keyword">可选的搜索关键字，用于筛选验方名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>符合条件的验方DTO列表</returns>
        Task<List<FormulaDto>> GetFormulaDtosAsync(string? keyword = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有启用状态的验方DTO
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有启用验方DTO列表，按名称排序</returns>
        Task<List<FormulaDto>> GetAllFormulaDtosAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取验方DTO详情（包含药材组成）
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验方详情DTO，如果不存在或已禁用则返回null</returns>
        Task<FormulaDto?> GetFormulaDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取验方模板DTO列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>共享验方模板DTO列表，用于处方开具时的模板选择</returns>
        Task<List<FormulaDto>> GetTemplateDtosAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据验方类型查询验方DTO
        /// </summary>
        /// <param name="formulaType">验方类型关键字，不能为空</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>指定类型验方DTO列表</returns>
        Task<List<FormulaDto>> GetFormulaDtosByTypeAsync(string formulaType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据关键字和分类查询验方DTO
        /// </summary>
        /// <param name="keyword">可选的搜索关键字，用于匹配验方名称或功效</param>
        /// <param name="category">可选的验方分类筛选条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>符合条件的验方DTO列表，支持多条件组合筛选</returns>
        Task<List<FormulaDto>> GetFormulaDtosAsync(string? keyword, string? category, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取验方分类列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有验方分类列表，包括经典验方、临床验方、个人验方</returns>
        Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查验方名称是否可用
        /// </summary>
        /// <param name="name">验方名称</param>
        /// <param name="excludeId">排除的验方ID（用于更新时检查）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>如果名称可用返回true，否则返回false</returns>
        Task<bool> IsNameAvailableAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取共享验方数量
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>共享验方的数量</returns>
        Task<int> GetSharedFormulaCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取最近创建的验方DTO列表
        /// </summary>
        /// <param name="count">返回的数量，默认10个</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最近创建的验方DTO列表</returns>
        Task<List<FormulaDto>> GetRecentFormulaDtosAsync(int count = 10, CancellationToken cancellationToken = default);
    }
}