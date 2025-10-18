using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IMedicalCaseRepository
    {
        Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<MedicalCaseDto?> GetByIdAsync(Guid id);
        Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto dto);
        Task<MedicalCaseDto> UpdateAsync(MedicalCaseUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);
        Task<MedicalCaseDto> CreateWithDetailsAsync(MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null);
        Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id);
    }
}
