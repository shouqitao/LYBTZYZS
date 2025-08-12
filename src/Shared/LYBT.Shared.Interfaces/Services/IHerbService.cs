using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Common;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 中药材服务接口 - 统一定义
    /// </summary>
    public interface IHerbService
    {
        Task<HerbDto> GetByIdAsync(Guid id);
        Task<PagedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query);
        Task<List<HerbDto>> GetAllAsync();
        Task<HerbDto> CreateAsync(HerbCreateDto dto);
        Task<HerbDto> UpdateAsync(Guid id, HerbUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<HerbDto>> GetByIdsAsync(List<Guid> ids);
        Task<bool> UpdateStockAsync(Guid id, HerbStockUpdateDto dto);
        Task<bool> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto);
        Task<HerbStockStatisticsDto> GetStockStatisticsAsync();
        Task<List<HerbDto>> SearchAsync(string keyword);
        Task<bool> BatchUpdateStatusAsync(BatchStatusUpdateDto dto);
    }
}