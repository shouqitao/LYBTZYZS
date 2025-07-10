using LYBT.Module.Prescriptions.Dtos;
using LYBT.Common.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    /// <summary>
    /// 处方相关 API
    /// </summary>
    public interface IPrescriptionApi {
        [Get("/api/Prescriptions")]
        Task<List<PrescriptionDto>> GetListAsync();

        [Get("/api/Prescriptions/{id}")]
        Task<PrescriptionDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Prescriptions")]
        Task<ApiSuccessResponse> AddAsync([Body] PrescriptionCreateDto dto);

        [Put("/api/Prescriptions")]
        Task<ApiSuccessResponse> UpdateAsync([Body] PrescriptionEditDto dto);

        [Delete("/api/Prescriptions/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);
    }
}
