using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 患者API服务接口
    /// </summary>
    public interface IPatientApiService
    {
        [Get("/api/v1/patients")]
        Task<LYBT.WPF.Client.Core.Models.ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
            [Query] int pageIndex = 1,
            [Query] int pageSize = 20,
            [Query] string? searchTerm = null);
        
        [Get("/api/v1/patients/{id}")]
        Task<LYBT.WPF.Client.Core.Models.ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);
        
        [Post("/api/v1/patients")]
        Task<LYBT.WPF.Client.Core.Models.ApiResponse<PatientDto>> CreatePatientAsync([Body] PatientCreateDto request);
        
        [Put("/api/v1/patients/{id}")]
        Task<LYBT.WPF.Client.Core.Models.ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, [Body] PatientUpdateDto request);
        
        [Delete("/api/v1/patients/{id}")]
        Task<LYBT.WPF.Client.Core.Models.ApiResponse> DeletePatientAsync(Guid id);
        
        // TODO: MedicalHistoryDto type not found, commented out for now
        //[Get("/api/v1/patients/{id}/history")]
        //Task<LYBT.WPF.Client.Core.Models.ApiResponse<List<MedicalHistoryDto>>> GetMedicalHistoryAsync(Guid id);
        
        [Get("/api/v1/patients/{id}/prescriptions")]
        Task<LYBT.WPF.Client.Core.Models.ApiResponse<List<PrescriptionDto>>> GetPrescriptionsAsync(Guid id);
    }
}