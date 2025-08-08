using LYBT.Shared.Models.Common;
using LYBT.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Interfaces
{
    public interface IMedicalCaseService
    {
        // 基本CRUD
        Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id);
        Task<List<MedicalCaseDto>> GetAllAsync();
        Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);
        Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseCreateDto dto);
        Task<bool> UpdateAsync(MedicalCaseEditDto dto);
        Task<bool> DeleteAsync(Guid id);

        // 工作流
        Task<List<MedicalCaseModel>> GetTodayByUserIdAsync(Guid userId);
        Task<bool> UpdateStatusAsync(Guid id, LYBT.Shared.Models.Enums.MedicalCaseStatus status);
        Task<bool> StartConsultationAsync(Guid caseId, Guid consultationId);
        Task<bool> CompleteConsultationAsync(Guid caseId, Guid? prescriptionId);
        Task<bool> CompleteMedicalCaseAsync(Guid id);
        Task<bool> CancelMedicalCaseAsync(Guid id, string reason);
        Task<List<MedicalCaseModel>> GetPendingCasesByStatusAsync(LYBT.Shared.Models.Enums.MedicalCaseStatus status);
        Task<(List<MedicalCaseModel> Items, int Total)> GetPagedAsync(int pageIndex, int pageSize, LYBT.Shared.Models.Enums.MedicalCaseStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);

        // 额外方法
        Task<bool> CompleteCaseAsync(Guid id);
    }
}