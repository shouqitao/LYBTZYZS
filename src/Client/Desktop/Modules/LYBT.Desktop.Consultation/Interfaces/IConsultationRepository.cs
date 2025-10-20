using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IConsultationRepository
    {
        Task<PagedResult<ConsultationDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<ConsultationDto?> GetByIdAsync(Guid id);
        Task<ConsultationDto> CreateAsync(ConsultationCreateDto dto);
        Task<ConsultationDto> UpdateAsync(ConsultationUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<ConsultationDto>> SearchAsync(string keyword);
        Task<List<ConsultationDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
        Task<ConsultationDto> StartAsync(Guid patientId);
    }
}
