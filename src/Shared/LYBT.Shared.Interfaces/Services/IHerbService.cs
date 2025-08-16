using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Common;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 中药材服务接口 - UltraThink统一标准
    /// </summary>
    public interface IHerbService
    {
        Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query);
        Task<ServiceResult<List<HerbDto>>> GetAllAsync();
        Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);
        Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids);
        Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto);
        Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto);
        Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync();
        Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
        Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto);
        
        // UltraThink P0修复：添加Client层期望的方法
        Task<ServiceResult<List<HerbDto>>> GetHerbsAsync();
        Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null);
        Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync();
        Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync();
        Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30);
        Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync();
        Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs);
        Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync();
        Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name);
    }
}