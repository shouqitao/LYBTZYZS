using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材仓储接口 - 继承IRepository&lt;Herb&gt;标准接口
    /// Phase 1 Task 1.4: 实现基础数据模块统一Repository规范
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - ⭐ 统一共性：继承IRepository&lt;Herb&gt;获得11个标准CRUD方法
    /// - ⭐ 保持特性：保留药材模块4个特定业务方法
    ///
    /// 特定业务方法说明：
    /// - GetByNameAsync: 精确名称查询（基础功能）
    /// - GetByNameOrPinyinAsync: 名称/拼音码模糊匹配（FormulaService引用）
    /// - ExistsByNameAsync: 名称重复检查（批量导入Epic #1962）
    /// - GetByCategoryAsync: 分类查询（分类管理Epic #1962）
    /// </remarks>
    public interface IHerbRepository : IRepository<Herb>
    {
        /// <summary>
        /// 根据名称精确获取药材
        /// </summary>
        /// <param name="name">药材名称</param>
        Task<Herb?> GetByNameAsync(string name);

        /// <summary>
        /// 按名称或拼音码查询药材 (Issue #1351)
        /// 优先精确匹配名称，其次模糊匹配拼音码
        /// 业务引用：FormulaService.TryMatchHerbAsync
        /// </summary>
        /// <param name="searchTerm">搜索词（药材名称或拼音码）</param>
        Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);

        /// <summary>
        /// 检查药材名称是否存在（支持排除指定ID，用于更新时验证）
        /// Epic #1962 Task 1.2: 批量导入重复检查
        /// 业务引用：HerbService.BatchImportAsync
        /// </summary>
        /// <param name="name">药材名称</param>
        /// <param name="excludeId">排除的ID（更新时传入当前记录ID）</param>
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

        // ========== OpenSpec: optimize-module-list-ui - 恢复功能支持 ==========

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// 用于Restore操作时获取已删除的实体
        /// </summary>
        /// <param name="id">实体ID</param>
        Task<Herb?> GetByIdIncludingDeletedAsync(Guid id);
    }
}
