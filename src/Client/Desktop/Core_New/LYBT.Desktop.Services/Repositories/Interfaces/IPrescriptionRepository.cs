using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Services.Repositories.Interfaces
{
    /// <summary>
    /// 处方数据仓储接口 - UltraThink架构
    /// </summary>
    public interface IPrescriptionRepository
    {
        Task<List<PrescriptionDto>> GetAllAsync();
        Task<PrescriptionDto> GetByIdAsync(Guid id);
        Task<PrescriptionDto> CreateAsync(PrescriptionDto prescription);
        Task<PrescriptionDto> UpdateAsync(PrescriptionDto prescription);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PrescriptionDto>> SearchAsync(string keyword);
        Task<List<PrescriptionDto>> GetByPatientIdAsync(Guid patientId);
        Task<List<PrescriptionDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<PrescriptionDto> DuplicatePrescriptionAsync(Guid prescriptionId);
    }
}