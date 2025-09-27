using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 中草药查询服务接口
    /// </summary>
    public interface IHerbQueryService
    {
        Task<PagedResult<HerbDto>> GetPagedHerbsAsync(HerbSearchDto searchDto);
        Task<HerbDto?> GetHerbByIdAsync(Guid herbId);
    }
}