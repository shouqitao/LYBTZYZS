using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Services.Repositories.Interfaces
{
    /// <summary>
    /// 病历数据仓储接口 - UltraThink架构
    /// </summary>
    public interface IMedicalCaseRepository
    {
        Task<List<MedicalCaseDto>> GetAllAsync();
        Task<MedicalCaseDto> GetByIdAsync(Guid id);
        Task<MedicalCaseDto> CreateAsync(MedicalCaseDto medicalCase);
        Task<MedicalCaseDto> UpdateAsync(MedicalCaseDto medicalCase);
        Task<bool> DeleteAsync(Guid id);
        Task<List<MedicalCaseDto>> SearchAsync(string keyword);
        Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);
        Task<List<MedicalCaseDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<MedicalCaseDto> GetLatestByPatientIdAsync(Guid patientId);
    }
}