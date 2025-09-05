using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Herbs.Interfaces
{

    /// <summary>
    /// 药材仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展药材特定业务方法
    /// </summary>
    public interface IHerbRepository : IBaseRepository<Herb>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义药材特有的业务方法

        /// <summary>
        /// 检查药材名称是否存在
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

        /// <summary>
        /// 根据拼音码搜索药材
        /// </summary>
        Task<List<Herb>> SearchByPinyinAsync(string pinyin);

        /// <summary>
        /// 批量新增药材
        /// </summary>
        Task<bool> AddRangeAsync(List<Herb> herbs);
    }
}
