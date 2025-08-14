using System.Threading.Tasks;
using System.Linq;
using System;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Prescriptions.Interfaces
{

    /// <summary>
    /// 处方业务接口定义
    /// </summary>
    public interface IPrescriptionService
    {

        Task<List<PrescriptionDto>> GetAllAsync();

        Task<PaginatedResult<PrescriptionDto>> GetPagedAsync(PagedQueryBaseDto query);

        Task<PrescriptionDetailDto?> GetByIdAsync(string id);

        Task<PrescriptionDto?> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName);

        Task<bool> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName);

        Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName);

        Task<bool> CancelAsync(string id, Guid operatorId, string operatorName);
    }
}