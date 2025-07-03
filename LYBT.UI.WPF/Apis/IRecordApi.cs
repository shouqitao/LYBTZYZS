using LYBT.Module.Records.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IRecordApi {
        [Get("/api/Record")]
        Task<List<RecordDto>> GetListAsync();

        [Get("/api/Record/patient/{patientId}")]
        Task<List<RecordDto>> GetByPatientIdAsync(Guid patientId);

        [Get("/api/Record/{id}")]
        Task<RecordDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Record")]
        Task<ApiSuccessResponse> AddAsync([Body] RecordCreateDto dto);

        [Put("/api/Record")]
        Task<ApiSuccessResponse> UpdateAsync([Body] RecordEditDto dto);

        [Delete("/api/Record/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);

        [Post("/api/Record/share/{id}")]
        Task<ApiSuccessResponse> MarkAsSharedAsync(Guid id, [Body] List<string> doctorIds);

        [Post("/api/Record/unshare/{id}")]
        Task<ApiSuccessResponse> RevokeSharingAsync(Guid id);

        [Get("/api/Record/shared/{doctorId}")]
        Task<List<RecordDto>> GetSharedRecordsAsync(Guid doctorId);
    }
}
