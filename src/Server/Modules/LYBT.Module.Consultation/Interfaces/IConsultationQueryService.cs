using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗查询服务接口
    /// </summary>
    public interface IConsultationQueryService
    {
        Task<PagedResult<ConsultationDto>> GetPagedConsultationsAsync(ConsultationSearchDto searchDto);
        Task<ConsultationDto?> GetConsultationByIdAsync(Guid consultationId);
    }
}