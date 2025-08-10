using System;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 患者API服务接口
    /// </summary>
    public interface IPatientApiService
    {
        [Get("/api/v1/patients")]
        Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
            [Query] int pageIndex = 1,
            [Query] int pageSize = 20,
            [Query] string? searchTerm = null);
        
        [Get("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);
        
        [Post("/api/v1/patients")]
        Task<ApiResponse<PatientDto>> CreatePatientAsync([Body] CreatePatientRequest request);
        
        [Put("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, [Body] UpdatePatientRequest request);
        
        [Delete("/api/v1/patients/{id}")]
        Task<ApiResponse> DeletePatientAsync(Guid id);
        
        [Get("/api/v1/patients/{id}/history")]
        Task<ApiResponse<List<MedicalHistoryDto>>> GetMedicalHistoryAsync(Guid id);
        
        [Get("/api/v1/patients/{id}/prescriptions")]
        Task<ApiResponse<List<PrescriptionDto>>> GetPrescriptionsAsync(Guid id);
    }
}