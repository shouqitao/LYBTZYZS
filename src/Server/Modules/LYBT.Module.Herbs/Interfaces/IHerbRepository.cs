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
    }
}