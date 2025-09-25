using System.ComponentModel;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{

    /// <summary>
    /// 药材仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展药材特定业务方法
    /// </summary>
    public interface IHerbRepository : IRepository<Herb>
    {
        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<PagedResult<Herb>> GetPagedAsync(HerbSearchDto query);
        
        /// <summary>
        /// 搜索药材
        /// </summary>
        Task<List<Herb>> SearchAsync(string keyword, int maxResults = 50);
        
        /// <summary>
        /// 根据ID列表获取药材
        /// </summary>
        Task<List<Herb>> GetByIdsAsync(List<Guid> ids);
        
        /// <summary>
        /// 根据分类获取药材
        /// </summary>
        Task<List<Herb>> GetByCategoryAsync(string category);
        
        /// <summary>
        /// 检查药材名称是否存在
        /// </summary>
        Task<bool> ExistsByNameAsync(string name);
    }
}
