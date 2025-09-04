using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// 职责：复杂查询、搜索、筛选、分页等只读操作
    /// </summary>
    public interface IHerbQueryService
    {
        /// <summary>
        /// 获取所有启用状态的药材列表
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetAllAsync();
        
        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query);
        
        /// <summary>
        /// 搜索药材（根据名称、拼音码）
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
        
        /// <summary>
        /// 获取可用药材列表（状态为启用）
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync();
        
        /// <summary>
        /// 根据ID列表批量获取药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids);
        
        /// <summary>
        /// 按价格区间查询药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        
        /// <summary>
        /// 根据名称精确查找药材
        /// </summary>
        Task<ServiceResult<HerbDto>> GetByNameAsync(string name);
        
        /// <summary>
        /// 获取热门药材（按使用频率排序）
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetPopularHerbsAsync(int count = 20);
    }
}