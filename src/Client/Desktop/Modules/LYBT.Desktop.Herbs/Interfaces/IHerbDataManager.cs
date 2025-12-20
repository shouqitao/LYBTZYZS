using LYBT.Desktop.Contracts.Components;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces
{
    /// <summary>
    /// 药材数据管理器接口
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IHerbDataManager : IDataManager<HerbDetailDto>
    {
        /// <summary>
        /// 分页查询药材列表
        /// </summary>
        Task<Shared.Models.Contracts.Common.PagedResult<HerbDetailDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchKeyword = null);
    }
}
