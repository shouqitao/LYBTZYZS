using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 中草药业务服务接口
    /// </summary>
    public interface IHerbService
    {
        Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);
        Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
        Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}