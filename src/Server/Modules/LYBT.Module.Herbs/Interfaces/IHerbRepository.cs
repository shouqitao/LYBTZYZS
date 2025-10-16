using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IHerbRepository : IRepository<Herb>
    {
        /// <summary>
        /// 根据名称获取药材
        /// </summary>
        Task<Herb?> GetByNameAsync(string name);

        /// <summary>
        /// 按名称或拼音码查询药材 (Issue #1351)
        /// 优先精确匹配名称，其次模糊匹配拼音码
        /// </summary>
        /// <param name="searchTerm">搜索词（药材名称或拼音码）</param>
        Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);
    }
}
