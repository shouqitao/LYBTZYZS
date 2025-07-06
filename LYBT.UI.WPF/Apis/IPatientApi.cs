using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IPatientApi {
        [Post("/api/Patients/add")]
        Task<bool> AddAsync([Body] PatientDetailDto dto);

        [Put("/api/Patients/edit")]
        Task<bool> UpdateAsync([Body] PatientDetailDto dto);

        [Put("/api/Patients/enable/{id}")]
        Task<bool> EnableAsync(Guid id);

        [Put("/api/Patients/disable/{id}")]
        Task<bool> DisableAsync(Guid id);

        [Get("/api/Patients/get/{id}")]
        Task<PatientDetailDto> GetByIdAsync(Guid id);

        [Get("/api/Patients/all")]
        Task<List<PatientDetailDto>> GetAllAsync();

        [Post("/api/Patients/paged")]
        Task<PagedResultDto<PatientDetailDto>> GetPagedAsync([Body] PatientPagedQueryDto query);

        [Post("/api/Patients/batchDelete")]
        Task<bool> BatchDeleteAsync([Body] List<string> ids);

        [Put("/api/Patients/batch-disable")]
        Task<bool> BatchDisableAsync([Body] BatchIdsDto dto);

        [Get("/api/Patients/search")]
        Task<List<PatientDetailDto>> SearchAsync([Query] string keyword);

        [Get("/api/Patients/doctor/{doctorId}")]
        Task<List<PatientDetailDto>> GetForDoctorAsync(Guid doctorId);

        [Post("/api/Patients/{id}/assign-doctor")]
        Task<bool> AssignDoctorAsync(Guid id, [Body] AssignDoctorDto dto);

        [Post("/api/Patients/import")]
        Task<bool> ImportAsync([Body] List<PatientDetailDto> dtos);

        [Post("/api/Patients/export")]
        Task<List<PatientDetailDto>> ExportAsync();

        [Get("/api/Patients/{id}/records")]
        Task<List<RecordDto>> GetHistoryAsync(Guid id);
    }
}
