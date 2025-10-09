using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Services.Repositories.Interfaces
{
    /// <summary>
    /// 诊疗数据仓储接口 - UltraThink架构
    /// </summary>
    public interface IConsultationRepository
    {
        Task<List<ConsultationDto>> GetAllAsync();
        Task<ConsultationDto> GetByIdAsync(Guid id);
        Task<ConsultationDto> CreateAsync(ConsultationDto consultation);
        Task<ConsultationDto> UpdateAsync(ConsultationDto consultation);
        Task<bool> DeleteAsync(Guid id);
        Task<List<ConsultationDto>> SearchAsync(string keyword);
        Task<List<ConsultationDto>> GetByPatientIdAsync(Guid patientId);
        Task<List<ConsultationDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
        Task<List<ConsultationDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ConsultationDto> GetLatestByPatientIdAsync(Guid patientId);
        Task<List<ConsultationDto>> GetTodayConsultationsAsync();
        Task<ConsultationDto> GetActiveConsultationAsync(Guid patientId);
        Task<bool> CompleteConsultationAsync(Guid consultationId);
    }
}
