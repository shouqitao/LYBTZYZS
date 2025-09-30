using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Services.Repositories.Interfaces
{
    /// <summary>
    /// 患者数据仓储接口 - UltraThink架构
    /// </summary>
    public interface IPatientRepository
    {
        Task<List<PatientDto>> GetAllAsync();
        Task<PatientDto> GetByIdAsync(Guid id);
        Task<PatientDto> CreateAsync(PatientDto patient);
        Task<PatientDto> UpdateAsync(PatientDto patient);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PatientDto>> SearchAsync(string keyword);
    }
}
