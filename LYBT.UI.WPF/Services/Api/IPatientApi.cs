using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services.Api {
    public interface IPatientApi {
        [Post("/api/Patients/add")]
        Task<ApiSuccessResponse> AddAsync([Body] PatientCreateDto dto);

        [Put("/api/Patients/edit")]
        Task<ApiSuccessResponse> UpdateAsync([Body] PatientEditDto dto);

        [Put("/api/Patients/enable/{id}")]
        Task<ApiSuccessResponse> EnableAsync(Guid id);

        [Put("/api/Patients/disable/{id}")]
        Task<ApiSuccessResponse> DisableAsync(Guid id);

        [Get("/api/Patients/get/{id}")]
        Task<PatientDetailDto> GetByIdAsync(Guid id);

        [Get("/api/Patients/all")]
        Task<List<PatientDto>> GetAllAsync();

        [Post("/api/Patients/paged")]
        Task<PagedResultDto<PatientDto>> GetPagedAsync([Body] PatientPagedQueryDto query);

        [Post("/api/Patients/batchDelete")]
        Task<ApiSuccessResponse> BatchDeleteAsync([Body] List<string> ids);

        [Put("/api/Patients/batch-disable")]
        Task<ApiSuccessResponse> BatchDisableAsync([Body] BatchIdsDto dto);

        [Get("/api/Patients/search")]
        Task<List<PatientDto>> SearchAsync([Query] string keyword);

        [Get("/api/Patients/doctor/{doctorId}")]
        Task<List<PatientDto>> GetForDoctorAsync(Guid doctorId);

        [Post("/api/Patients/{id}/assign-doctor")]
        Task<ApiSuccessResponse> AssignDoctorAsync(Guid id, [Body] AssignDoctorDto dto);

        [Post("/api/Patients/import")]
        Task<ApiSuccessResponse> ImportAsync([Body] List<PatientCreateDto> dtos);

        [Post("/api/Patients/export")]
        Task<List<PatientDto>> ExportAsync();

        [Get("/api/Patients/{id}/records")]
        Task<List<RecordDto>> GetHistoryAsync(Guid id);
    }
}
