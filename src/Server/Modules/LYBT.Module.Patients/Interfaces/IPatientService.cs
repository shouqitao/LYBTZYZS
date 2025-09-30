using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者业务服务接口
    /// </summary>
    public interface IPatientService
    {
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
        Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}