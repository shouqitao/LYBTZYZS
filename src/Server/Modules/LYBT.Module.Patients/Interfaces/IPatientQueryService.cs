using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者查询服务接口
    /// </summary>
    public interface IPatientQueryService
    {
        Task<PagedResult<PatientDto>> GetPagedPatientsAsync(PatientSearchDto searchDto);
        Task<PatientDto?> GetPatientByIdAsync(Guid patientId);
    }
}