using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗业务服务接口
    /// </summary>
    public interface IConsultationService
    {
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto);
        Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto);
        Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}