using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Repositories
{
    /// <summary>
    /// 药材数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IHerbRepository
    {
        Task<PagedResult<HerbDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<HerbDto> GetByIdAsync(Guid id);
        Task<HerbDto> CreateAsync(HerbCreateDto dto);
        Task<HerbDto> UpdateAsync(HerbUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<HerbDto>> SearchAsync(string keyword);
    }
}
