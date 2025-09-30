using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Services.Repositories.Interfaces
{
    /// <summary>
    /// 草药数据仓储接口 - UltraThink架构
    /// </summary>
    public interface IHerbRepository
    {
        Task<List<HerbDto>> GetAllAsync();
        Task<HerbDto> GetByIdAsync(Guid id);
        Task<HerbDto> CreateAsync(HerbDto herb);
        Task<HerbDto> UpdateAsync(HerbDto herb);
        Task<bool> DeleteAsync(Guid id);
        Task<List<HerbDto>> SearchAsync(string keyword);
        Task<List<HerbDto>> GetByCategoryAsync(string category);
        Task<List<HerbDto>> GetFrequentlyUsedAsync(int limit = 10);
    }
}