using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IDiagnosisTreatmentApi {
        [Get("/api/DiagnosisTreatment")]
        Task<List<DiagnosisTreatmentDto>> GetListAsync();

        [Get("/api/DiagnosisTreatment/{id}")]
        Task<DiagnosisTreatmentDetailDto> GetByIdAsync(Guid id);

        [Post("/api/DiagnosisTreatment")]
        Task<ApiSuccessResponse> AddAsync([Body] DiagnosisTreatmentCreateDto dto);

        [Put("/api/DiagnosisTreatment")]
        Task<ApiSuccessResponse> UpdateAsync([Body] DiagnosisTreatmentEditDto dto);

        [Delete("/api/DiagnosisTreatment/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);
    }
}
